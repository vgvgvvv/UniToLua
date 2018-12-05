

MyClass = TestUniToLua.TestClasses.MyClass

local mc = MyClass.New()

print(MyClass.staticField)
print(MyClass.staticProperty)

print(MyClass.StaticFunction(MyClass.staticField, MyClass.staticProperty))

print(mc.memberField)
print(mc.memberProperty)

print(mc:MemberFunction(mc.memberField, mc.memberProperty))