
namespace UniLua
{
	internal class LuaDebugLib
	{
		public const string LIB_NAME = "debug";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "traceback", 	DBG_Traceback	),
                new NameFuncPair( "dumpstack",  DumpStack   ),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int DBG_Traceback( ILuaState lua )
		{
			return 0;
		}

        private static int DumpStack(ILuaState lua)
        {

            lua.DumpStack();

			return 0;
		}
	}
}

