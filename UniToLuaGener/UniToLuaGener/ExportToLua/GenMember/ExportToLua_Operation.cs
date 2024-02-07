
// ┌──────────────────────────┬───────────────────────┬──────────────────────────┐
// │         Operator         │      Method Name      │       Description        │
// ├──────────────────────────┼───────────────────────┼──────────────────────────┤
// │ operator +               │ op_UnaryPlus          │ Unary                    │
// │ operator -               │ op_UnaryNegation      │ Unary                    │
// │ operator ++              │ op_Increment          │                          │
// │ operator --              │ op_Decrement          │                          │
// │ operator !               │ op_LogicalNot         │                          │
// │ operator +               │ op_Addition           │                          │
// │ operator -               │ op_Subtraction        │                          │
// │ operator *               │ op_Multiply           │                          │
// │ operator /               │ op_Division           │                          │
// │ operator &               │ op_BitwiseAnd         │                          │
// │ operator |               │ op_BitwiseOr          │                          │
// │ operator ^               │ op_ExclusiveOr        │                          │
// │ operator ~               │ op_OnesComplement     │                          │
// │ operator ==              │ op_Equality           │                          │
// │ operator !=              │ op_Inequality         │                          │
// │ operator <               │ op_LessThan           │                          │
// │ operator >               │ op_GreaterThan        │                          │
// │ operator <=              │ op_LessThanOrEqual    │                          │
// │ operator >=              │ op_GreaterThanOrEqual │                          │
// │ operator <<              │ op_LeftShift          │                          │
// │ operator >>              │ op_RightShift         │                          │
// │ operator %               │ op_Modulus            │                          │
// │ implicit operator <type> │ op_Implicit           │ Implicit type conversion │
// │ explicit operator <type> │ op_Explicit           │ Explicit type conversion │
// │ operator true            │ op_True               │                          │
// │ operator false           │ op_False              │                          │
// └──────────────────────────┴───────────────────────┴──────────────────────────┘

using System.Collections.Generic;

namespace UniToLuaGener
{
    public class OperationInfo
    {
        public string OperatorChar;
        public string CsFuncName;
        public string LuaFuncName;
        public string InvokeStringFormat;
        
        public static Dictionary<string, OperationInfo> OperationInfos = new Dictionary<string, OperationInfo>()
        {
            ["op_UnaryPlus"] = new OperationInfo(){ CsFuncName = "op_UnaryPlus", OperatorChar = "+", InvokeStringFormat = "+{0}", LuaFuncName = "UnaryPlus"},
            ["op_UnaryNegation"] = new OperationInfo(){CsFuncName = "op_UnaryNegation", OperatorChar = "-", InvokeStringFormat = "-{0}", LuaFuncName = "UnaryNegation"},
            ["op_Increment"] = new OperationInfo(){CsFuncName = "op_Increment", OperatorChar = "++", InvokeStringFormat = "++{0}", LuaFuncName = "Increment"},
            ["op_Decrement"] = new OperationInfo(){CsFuncName = "op_Decrement", OperatorChar = "--", InvokeStringFormat = "--{0}", LuaFuncName = "Decrement"},
            ["op_LogicalNot"] = new OperationInfo(){CsFuncName = "op_LogicalNot", OperatorChar = "!", InvokeStringFormat = "!{0}", LuaFuncName = "Not"},
            ["op_Addition"] = new OperationInfo(){CsFuncName = "op_Addition", OperatorChar = "+", InvokeStringFormat = "{0} + {1}", LuaFuncName = "__add"},
            ["op_Subtraction"] = new OperationInfo(){CsFuncName = "op_Subtraction", OperatorChar = "-", InvokeStringFormat = "{0} - {1}", LuaFuncName = "__sub"},
            ["op_Multiply"] = new OperationInfo(){CsFuncName = "op_Multiply", OperatorChar = "*", InvokeStringFormat = "{0} * {1}", LuaFuncName = "__mul"},
            ["op_Division"] = new OperationInfo(){CsFuncName = "op_Division", OperatorChar = "/", InvokeStringFormat = "{0} / {1}", LuaFuncName = "__div"},
            ["op_BitwiseAnd"] = new OperationInfo(){CsFuncName = "op_BitwiseAnd", OperatorChar = "&", InvokeStringFormat = "{0} & {1}", LuaFuncName = "BitwiseAnd"},
            ["op_BitwiseOr"] = new OperationInfo(){CsFuncName = "op_BitwiseOr", OperatorChar = "|", InvokeStringFormat = "{0} | {1}", LuaFuncName = "BitwiseOr"},
            ["op_ExclusiveOr"] = new OperationInfo(){CsFuncName = "op_ExclusiveOr", OperatorChar = "^", InvokeStringFormat = "{0} ^ {1}", LuaFuncName = "ExclusiveOr"},
            ["op_OnesComplement"] = new OperationInfo(){CsFuncName = "op_OnesComplement", OperatorChar = "~", InvokeStringFormat = "~{0}", LuaFuncName = "OnesComplement"},
            ["op_Equality"] = new OperationInfo(){CsFuncName = "op_Equality", OperatorChar = "==", InvokeStringFormat = "{0} == {1}", LuaFuncName = "__eq"},
            ["op_Inequality"] = new OperationInfo(){CsFuncName = "op_Inequality", OperatorChar = "!=", InvokeStringFormat = "{0} != {1}", LuaFuncName = "Inequality"},
            ["op_LessThan"] = new OperationInfo(){CsFuncName = "op_LessThan", OperatorChar = "<", InvokeStringFormat = "{0} < {1}", LuaFuncName = "__lt"},
            ["op_GreaterThan"] = new OperationInfo(){CsFuncName = "op_GreaterThan", OperatorChar = ">", InvokeStringFormat = "{0} > {1}", LuaFuncName = "GreaterThan"},
            ["op_LessThanOrEqual"] = new OperationInfo(){CsFuncName = "op_LessThanOrEqual", OperatorChar = "<=", InvokeStringFormat = "{0} <= {1}", LuaFuncName = "__le"},
            ["op_GreaterThanOrEqual"] = new OperationInfo(){CsFuncName = "op_GreaterThanOrEqual", OperatorChar = ">=", InvokeStringFormat = "{0} >= {1}", LuaFuncName = "GreaterThanOrEqual"},
            ["op_LeftShift"] = new OperationInfo(){CsFuncName = "op_LeftShift", OperatorChar = "<<", InvokeStringFormat = "{0} << {1}", LuaFuncName = "LeftShift"},
            ["op_RightShift"] = new OperationInfo(){CsFuncName = "op_RightShift", OperatorChar = ">>", InvokeStringFormat = "{0} >> {1}", LuaFuncName = "RightShift"},
            ["op_Modulus"] = new OperationInfo(){CsFuncName = "op_Modulus", OperatorChar = "%", InvokeStringFormat = "{0} % {1}", LuaFuncName = "__mod"},
            ["op_Implicit"] = new OperationInfo(){CsFuncName = "op_Implicit", OperatorChar = string.Empty, InvokeStringFormat = "({type}){0}", LuaFuncName = "Implicit"},
            ["op_Explicit"] = new OperationInfo(){CsFuncName = "op_Explicit", OperatorChar = string.Empty, InvokeStringFormat = "({type}){0}", LuaFuncName = "Explicit"},
            // ["op_True"] = new OperationInfo(){CsFuncName = "op_True", OperatorChar = "true", InvokeStringFormat = "{type}{0}", LuaFuncName = "UnaryPlus"},
            // ["op_False"] = new OperationInfo(){CsFuncName = "op_False", OperatorChar = "false", InvokeStringFormat = "+{0}", LuaFuncName = "UnaryPlus"},
        };

        public static bool IsOperation(string methodName)
        {
            return OperationInfos.ContainsKey(methodName);
        }
    }
    
    public class ExportToLua_Operation
    {
        
    }
}