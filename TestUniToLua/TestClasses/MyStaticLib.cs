using UniLua;

namespace TestUniToLua.TestClasses
{
    [ToLua]
    public static class MyStaticLib
    {
        public static int field;

        public static int property { get; set; }

        public static int Function(int a, int b)
        {
            return a + b;
        }
    }
}