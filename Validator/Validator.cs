#region using

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AoOni.MockEnum;

#endregion

namespace Validator
{
	[MockEnumFlags]
	public sealed class ErrorType : MockEnumBase<int, ErrorType>
	{
		#region Static Constructor

		static ErrorType() { CreateInstanceList(); }

		#endregion

		#region Members

		[AoOni.MockEnum.MockEnumMember(0, "No Errors")]
		public static readonly ErrorType NoErrors;

		[AoOni.MockEnum.MockEnumMember(1, "Is Empty")]
		public static readonly ErrorType IsEmpty;

		[AoOni.MockEnum.MockEnumMember(2, "Too Long")]
		public static readonly ErrorType TooLong;

		[AoOni.MockEnum.MockEnumMember(4, "Invalid Characters")]
		public static readonly ErrorType InvalidCharacters;

		[AoOni.MockEnum.MockEnumMember(8, "Invalid Yubin")]
		public static readonly ErrorType InvalidYubin;

		[AoOni.MockEnum.MockEnumMember(16, "Not Same")]
		public static readonly ErrorType NotSame;

		#endregion
	}

	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class ValidatorClassAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = true)]
	public class ValidatorMethodAttribute : Attribute { }

	[ValidatorClass]
	public abstract class ValidatorBase
	{
		[ValidatorMethod]
		public abstract ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList);
	}

	public class CheckIsNotEmptyValidator : ValidatorBase
	{
		public override ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList)
		{
			return !string.IsNullOrWhiteSpace((dataSet[fieldId] ?? string.Empty)) ? ErrorType.NoErrors : ErrorType.IsEmpty;
		}
	}

	public class CheckMaxLengthValidator : ValidatorBase
	{
		public override ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList)
		{
			return (dataSet[fieldId] ?? string.Empty).Trim().Length <= int.Parse(extraArgList[0]) ? ErrorType.NoErrors : ErrorType.TooLong;
		}
	}

	public class CheckIsAlphabeticValidator : ValidatorBase
	{
		public override ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList)
		{
			return Regex.IsMatch((dataSet[fieldId] ?? string.Empty).Trim(), "^[A-Za-z]+$") ? ErrorType.NoErrors : ErrorType.InvalidCharacters;
		}
	}

	public class CheckIsYubinNoValidator : ValidatorBase
	{
		public override ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList)
		{
			return Regex.IsMatch((dataSet[fieldId] ?? string.Empty).Trim(), @"^\d{3}-\d{4}$") ? ErrorType.NoErrors : ErrorType.InvalidYubin;
		}
	}

	public class CheckIsSameValidator : ValidatorBase
	{
		public override ErrorType Validate(Dictionary<string, string> dataSet, string fieldId, string[] extraArgList)
		{
			return ((dataSet[fieldId] ?? string.Empty).Trim() == (dataSet[extraArgList[0]] ?? string.Empty).Trim()) ? ErrorType.NoErrors : ErrorType.NotSame;
		}
	}
}
