namespace UniLua
{
    public partial class LuaState
    {
        public const int TOLUA_PRELOAD = 21;      //preload
        public const int TOLUA_LOADED = 22;       //loaded
        public const int TOLUA_MAINTHREAD = 23;   //mainthread
        public const int TOLUA_GLOBAL = 24;       //global
        public const int TOLUA_REQUIRE = 25;      //require

        public void OpenToLua()
        {
            
            //API.NewTable();             //table
            //API.SetGlobal("tolua");     //

            OpenCacheLuaVar();
            OpenPreload();
        }


        private void OpenCacheLuaVar()
        {
            API.PushThread();
            API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_MAINTHREAD);

            //API.PushValue(LuaDef.LUA_RIDX_GLOBALS);
            //API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_GLOBAL);

            API.GetGlobal("require");
            API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_REQUIRE);
        }

        private void OpenPreload()
        {
            API.GetGlobal("package");     //table

            API.PushString("preload");  //table preload

            API.RawGet(-2);             //table tprel
            API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_PRELOAD); //table 

            //API.NewTable();             //table preload table
            //API.PushValue(-1);          //table preload table table
            //API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_PRELOAD); //table preload table
            //API.RawSet(-3);             //table

            API.PushString("loaded");  //table preload
            API.RawGet(-2);             //table tload
            API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_LOADED); //table

            //API.NewTable();             //table preload table
            //API.PushValue(-1);          //table preload table table
            //API.RawSetI(LuaDef.LUA_REGISTRYINDEX, TOLUA_LOADED); //table preload table
            //API.RawSet(-3);             //table

            API.Pop(1);
            

        }
    }
}