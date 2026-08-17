namespace Member.KYM.Scripts.CoreSystems
{
    public class CodeFormat
    {
        public static string EnumFormat =
            @"
namespace {0}
{{
    public enum {1}
    {{
        {2}
    }}
}}
";
    }
}