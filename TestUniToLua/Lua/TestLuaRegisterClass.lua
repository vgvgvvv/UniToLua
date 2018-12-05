
local t1 = Test.TestClass.New()
local t2 = Test.TestClass.New()
print(t1.value)
t1.value = t1.value + 200
print(t1.value)
print(t2:Add(100, 200))
print(t2.value)
--local mt = getmetatable(t)

--for k,v in pairs(mt[".get"]) do
--	print(k,v)
--end

--print(mt[".get"].value(t))
