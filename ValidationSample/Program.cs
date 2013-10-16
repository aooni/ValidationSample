#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using Validator;

#endregion

namespace ValidationSample
{
	class Program
	{
		private static IEnumerable<Type> _validatorClassTypeArray = AppDomain.CurrentDomain.GetAssemblies()
			.First(assembly => assembly.GetName().Name == "Validator").GetTypes();

		private static ParameterExpression[] _validatorParameterExpressionArray = _validatorClassTypeArray
			.First(type => type == typeof(ValidatorBase)).GetMethods()
			.First(methodInfo => methodInfo.GetCustomAttributes(false).Any(attributes => attributes is ValidatorMethodAttribute)).GetParameters()
			.Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();

		private static IEnumerable<Type> _validatorSubstanceClassTypeArray = _validatorClassTypeArray
			.Where(type => type.IsClass && !type.IsAbstract && type.GetCustomAttributes(true).Any(attribute => attribute is ValidatorClassAttribute));

		private static Dictionary<Type, ValidatorBase> _validatorInstanceDictionary = _validatorSubstanceClassTypeArray
			.Select(validatorClassType => new { Key = validatorClassType, Value = Activator.CreateInstance(validatorClassType) as ValidatorBase })
			.ToDictionary(item => item.Key, item => item.Value);

		private static Dictionary<string, Func<Dictionary<string, string>, string, string[], ErrorType>> _validatorMethodDictionary =
			_validatorSubstanceClassTypeArray
			.Select(type => new
				{
					Key = type.Name,
					Value = Expression.Lambda(
							Expression.Call(
								Expression.Constant(_validatorInstanceDictionary[type]),
								type.GetMethods().First(methodInfo => methodInfo.GetCustomAttributes(true).Any(attributes => attributes is ValidatorMethodAttribute)),
								_validatorParameterExpressionArray
							),
							_validatorParameterExpressionArray
						).Compile() as Func<Dictionary<string, string>, string, string[], ErrorType>
				})
			.ToDictionary(item => item.Key, item => item.Value);

		private static Dictionary<string, Func<Dictionary<string, string>, string, ErrorType>[]> _validationRuleDictionary =
			XElement.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "validationRuleList.config"))
			.Element("validationRuleList").Elements("validationRule")
			.Select(validationRule => new
				{
					Key = validationRule.Element("fieldId").Value,
					Value = validationRule.Element("validatorList").Elements("validator")
						.Select(validator =>
							{
								return (Func<Dictionary<string, string>, string, ErrorType>)((dataSet, fieldId) => _validatorMethodDictionary[validator.Element("name").Value](dataSet, fieldId, validator.Element("extraArgList") == null ? null : validator.Element("extraArgList").Elements("extraArg").Select(element => element.Value).ToArray()));
							})
						.ToArray()
				})
			.ToDictionary(item => item.Key, item => item.Value);

		static void Main(string[] args)
		{
			#region sample data

			var sampleData = new Dictionary<string, string>()
			{
				{"name", "Dikembe Mutombo Mpolondo Mukamba Jean Jacque Wamutombo"},
				{"id", "モンデュー　アデュー　カプリス・デ・デュー"},
				{"address", "テスト県テスト市テスト町1-2-3"},
				{"yubin", "000-9988"},
				{"mail", "aaa@bbb.ccc.dddd"},
				{"mail2", "aaa@bbb.ccc.ddde"},
			};

			#endregion

			Array.ForEach(
				_validationRuleDictionary
					.SelectMany(
						validationList => validationList.Value,
						(validationList, validation) => new { Field = validationList.Key, Error = validation(sampleData, validationList.Key) }
					)
					.Where(result => result.Error != ErrorType.NoErrors).ToArray(),
				error => { Console.WriteLine(string.Format("Field:{0},Error:{1}", error.Field, error.Error.ToString())); }
			);

			Console.ReadKey();
		}
	}
}
