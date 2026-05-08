--region LuaCodeWriter
local LuaCodeWriter = fclass()

function LuaCodeWriter:ctor(config)
    config = config or {}
    self.blockStart = config.blockStart or '{'
    self.blockEnd = config.blockEnd or '}'
    self.blockFromNewLine = config.blockFromNewLine
    if self.blockFromNewLine == nil then
        self.blockFromNewLine = true
    end
    if config.usingTabs then
        self.indentStr = '\t'
    else
        self.indentStr = '    '
    end
    self.usingTabs = config.usingTabs
    self.endOfLine = config.endOfLine or '\n'
    self.lines = {}
    self.indent = 0

    self:writeMark()
end

function LuaCodeWriter:writeMark()
    -- table.insert(self.lines, '--- This is an automatically generated class by FairyGUI. Please do not modify it. ---')
    -- table.insert(self.lines, '')
end

function LuaCodeWriter:writeln(format, ...)
    if not format then
        table.insert(self.lines, '')
        return
    end

    local str = ''
    for i = 0, self.indent - 1 do
        str = str .. self.indentStr
    end
    str = str .. string.format(format, ...)
    table.insert(self.lines, str)

    return self
end

function LuaCodeWriter:startBlock()
    if self.blockFromNewLine or #self.lines == 0 then
        self:writeln(self.blockStart)
    else
        local str = self.lines[#self.lines]
        self.lines[#self.lines] = str .. ' ' .. self.blockStart
    end
    self.indent = self.indent + 1

    return self
end

function LuaCodeWriter:endBlock()
    self.indent = self.indent - 1
    self:writeln(self.blockEnd)

    return self
end

function LuaCodeWriter:incIndent()
    self.indent = self.indent + 1

    return self
end

function LuaCodeWriter:decIndent()
    self.indent = self.indent - 1

    return self
end

function LuaCodeWriter:reset()
    if #self.lines > 0 then
        self.lines = {}
    end
    self.indent = 0

    self:writeMark()
end

function LuaCodeWriter:tostring()
    return table.concat(self.lines, self.endOfLine)
end

function LuaCodeWriter:save(filePath)
    local str = table.concat(self.lines, self.endOfLine)

    CS.System.IO.File.WriteAllText(filePath, str)
end

--endregion

local pathSep = package.config:sub(1, 1)
local resMap = {}
local allListRenderMap = {}
local allExportedViewMap = {}
local defineLines = ""
local setVarLines = ""
local onClickFuncLines = ""

local compPrefix = {"comp", "img", "btn", "label", "progress", "slider", "combo", "txt", "rtxt", "input", "loader", "loader3D", "list", "graph", "ctrl", "trans", "scroll","group"}



local function _getExtension(res)
    if not res then
        return nil
    end
    local abc = CS.FairyEditor.ComponentAsset(res)
    return abc.extension
end

local function _getMemberType(memberInfo)
    local memberType = memberInfo.type
    if memberInfo.res and memberInfo.res.type == "component" then
        if not memberInfo.res.exported then
            local fieldExtention = _getExtension(memberInfo.res) 
            memberType = (fieldExtention and fieldExtention ~= "") and "G"..fieldExtention or "GComponent"
        else
            memberType = memberInfo.res.name
        end
    end
    return memberType
end

local function _toFuncName(name)
    local firstChar = string.sub(name, 1, 1)
    local restChars = string.sub(name, 2)
    return "OnClick" .. string.upper(firstChar) .. restChars
end

local function _firstUpper(str)
    local firstChar = string.sub(str, 1, 1)
    local restChars = string.sub(str, 2)
    return string.upper(firstChar) .. restChars
end

local function _inLs(ls, value)
    for _, v in pairs(ls) do
        if v == value then
            return true
        end
    end
    return false
end

--格式缩进
local _indent = "    " -- 4个空格
local function _createIndent(indentNum)
    local str = ""
    for i = 1, indentNum do
        str = str .. _indent
    end
    return str
end

--附加字符串
local function _appendStr(rawStr, appendStr, indentNum)
    if rawStr == "" then
        return appendStr
    else
        return rawStr .. "\n" .. _createIndent(indentNum) .. appendStr
    end
end

--判断变量是否需要在代码中定义
local function _checkIsValidMember(varName)
    for _, str in pairs(compPrefix) do
        if string.sub(varName, 1, #str) == str then
            return true
        end
    end
    return false
end

local function _getGlistDefaultItemName(handler, classInfo, memberInfo)
    local res = resMap[classInfo.className]
    local xmlInfo = handler:GetItemDesc(res)
    local ds = xmlInfo:GetNode("displayList")
    local ls = ds:Elements("list")
    local rawList = ls.rawList
    for i = 0, rawList.Count - 1 do
        local e = rawList[i]
        local name = e:GetAttribute("name")
        if name == memberInfo.name then
            local defaultItem = e:GetAttribute("defaultItem", "empty")
            if defaultItem ~= "empty" then
                local item = handler.project:GetItemByURL(defaultItem)
                if item == null then
                    fprint(string.format("defaultItem %s is not found name:%s class:%s",
                     defaultItem, name, classInfo.className))
                    return ""
                end
                if item.exported then
                    return item.name
                end
            end
        end
    end
    return ""
end

--@FKing230630
--读取xml获取空间自定义数据
local function _getCustomData(handler, classInfo, memberInfo)
    local res = resMap[classInfo.className]
    local xmlInfo = handler:GetItemDesc(res)
    local ds = xmlInfo:GetNode("displayList")
    local ls = ds:Elements("component")
    local rawList = ls.rawList
    for i = 0, rawList.Count - 1 do
        local e = rawList[i]
        local name = e:GetAttribute("name")
        if name == memberInfo.name then
            local customData = e:GetAttribute("customData", "empty")
            if customData ~= "empty" then
				--fprint(string.format("customData = %s , %s , %s", memberInfo.name, memberInfo.varName, customData))
				return customData
            end
        end
    end
    return ""
end

--lua中字符串拆分处理split函数实现
--lua中没有split直接拆分字符串的方法只有sub
--local fruits = _stringSplit("apple,banana,cherry", ",")
--fprint(string.format("%s ", fruits[1]))
local function _stringSplit(szFullString, szSeparator)
	--fprint(string.format("szFullString: %s", string.upper(szFullString)))
	--fprint(string.format("szFullString: %s", string.lower(szFullString)))
	local nFindStartIndex = 1
	local nSplitIndex = 1
	local nSplitArray = {}
	while true do
		local nFindLastIndex = string.find(szFullString, szSeparator, nFindStartIndex)
		if not nFindLastIndex then
			nSplitArray[nSplitIndex] = string.sub(szFullString, nFindStartIndex, string.len(szFullString))
			break
		end
		nSplitArray[nSplitIndex] = string.sub(szFullString, nFindStartIndex, nFindLastIndex - 1)
		nFindStartIndex = nFindLastIndex + string.len(szSeparator)
		nSplitIndex = nSplitIndex + 1
	end
	return nSplitArray
end
--@FKing230630

local function _getMemberXmlElement(handler, classRes, memberName, elementType)
    local xmlInfo = handler:GetItemDesc(classRes)
    local ds = xmlInfo:GetNode("displayList")
    local ls = ds:Elements(elementType)
    if ls.Count <= 0 then
        return nil
    end
    local rawList = ls.rawList
    for i = 0, rawList.Count - 1 do
        local e = rawList[i]
        local name = e:GetAttribute("name")
        if name == memberName then
            return e
        end
    end
end

-- local function _getExtUrl(handler, classRes, memberName, elementType)
--     local memEleXml = _getMemberXmlElement(handler, classRes, memberName, elementType)
--     if not memEleXml then
--         CUtil.error("错误", "%s 的 %s 找不到Xml资源", classRes.name, memberName)
--     end
--     local url = memEleXml:GetAttribute("url")
--     return url
-- end

-- local function _checkAndWriteExtUrl(handler, classInfo, memberInfo, fieldName)
--     if memberInfo.type == "GLoader" then
--         local url = _getExtUrl(handler, classInfo.res, fieldName, "loader")
--         if url then
--             local res = handler.project:GetItemByURL(url)
--             if res then
--                 if res.owner.name == "Textures" then
--                     --local pathAndFile = string.format("%s%s", res.path, res.name)
--                     --local extPath = pathAndFile:gsub("/Texture/", "")
--                     setVarLines = _appendStr(setVarLines, string.format('_%s.url = "%s";', memberInfo.varName, res.name), 3)
--                 end
--             end
--         end
--     end
-- end

local function _writeFunctions(handler, codePkgName, classInfo)
    local className = classInfo.className
    local members = classInfo.members
    local memCount = members.Count
    local controls = {}
    local buttons = {}
    local memberMap = {}
    local memberNameLs = {}
    for j = 0, memCount - 1 do
        local memberInfo = members[j]
        if memberInfo.res then
            CUtil.assertCompNotCrossPackage(codePkgName,  memberInfo.res)
        end
        if _checkIsValidMember(memberInfo.varName) then
            local fieldName = memberInfo.name
            table.insert(memberNameLs, fieldName)
            if memberMap[fieldName] then
                CUtil.error("错误", "%s 包的 %s 组件 有两个同名的字段%s", codePkgName, className, fieldName)
            end
            memberMap[fieldName] = memberInfo
        end
    end
    defineLines = ""
    setVarLines = ""
    onClickFuncLines = ""
    table.sort(memberNameLs)
    for _, fieldName in ipairs(memberNameLs) do
        local memberInfo = memberMap[fieldName]
        -- 使用首字母大写的字段名（不带下划线）
        local publicVarName = _firstUpper(memberInfo.varName)
        if memberInfo.group == 0 then
            local memberType = _getMemberType(memberInfo)
            defineLines = _appendStr(defineLines, string.format("public %s %s;", memberType, publicVarName), 2)
            if memberType == "GList" then
                --如果是GList，把它的默认item添加到注释后面去
                local defaultItemName = _getGlistDefaultItemName(handler, classInfo, memberInfo)
                if defaultItemName ~= "" then
                    setVarLines = _appendStr(setVarLines, string.format('%s = (%s)GetChild("%s");//default item: %s', publicVarName, memberType, memberInfo.name, defaultItemName), 0)
                else
                    setVarLines = _appendStr(setVarLines, string.format('%s = (%s)GetChild("%s");', publicVarName, memberType, memberInfo.name), 0)
                end
            else
                setVarLines = _appendStr(setVarLines, string.format('%s = (%s)GetChild("%s");', publicVarName, memberType, memberInfo.name), 0)
            end

            if _getExtension(memberInfo.res) == "Button" then
                table.insert(buttons, memberInfo)
            end
--             _checkAndWriteExtUrl(handler, classInfo, memberInfo, fieldName)
        elseif memberInfo.group == 1 then
            defineLines = _appendStr(defineLines, string.format("public %s %s;", memberInfo.type, publicVarName), 2)
            setVarLines = _appendStr(setVarLines, string.format('%s = GetController("%s");', publicVarName, memberInfo.name), 0)
        else
            defineLines = _appendStr(defineLines, string.format("public %s %s;", memberInfo.type, publicVarName), 2)
            setVarLines = _appendStr(setVarLines, string.format('%s = GetTransition("%s");', publicVarName, memberInfo.name), 0)
        end
    end
    -- for _, button in ipairs(buttons) do
    --     local funcName = _toFuncName(button.varName, "Click")
    --     setVarLines = _appendStr(setVarLines, string.format("%s.onClick.Set(%s);", "_" .. button.varName, funcName), 0)
    --     onClickFuncLines = _appendStr(onClickFuncLines, string.format("private void %s(){}", funcName), 2)
    -- end
end

---@param handler CS.FairyEditor.PublishHandler
---@param classInfo CS.FairyEditor.PublishHandler.ClassInfo
local function _writerWindow(handler, writer, codePkgName, tempCodePath, classInfo)
    local className = classInfo.className
    writer:reset()

    writer:writeln("// <auto-generated>")
    writer:writeln("// This code was generated by FairyGUI GenCode plugin.")
    writer:writeln("// Do not modify manually - changes will be overwritten.")
    writer:writeln("// </auto-generated>")
    writer:writeln()
    writer:writeln("using FairyGUI;")
    writer:writeln()
    writer:writeln("namespace GameLogic")
    writer:startBlock()
    writer:writeln("/// <summary>")
    writer:writeln("/// %s 窗口设计类 - 自动生成，请勿手动修改。", className)
    writer:writeln("/// 业务逻辑请在 %s.cs 中实现。", className)
    writer:writeln("/// </summary>")
    writer:writeln("public partial class %s : FairyUIWindow", className)
    writer:startBlock()
    
    -- 生成常量
    writer:writeln('/// <summary>')
    writer:writeln('/// 包名称常量。')
    writer:writeln('/// </summary>')
    writer:writeln('public const string PackageNameConst = "%s";', codePkgName)
    writer:writeln()
    writer:writeln('/// <summary>')
    writer:writeln('/// 组件名称常量。')
    writer:writeln('/// </summary>')
    writer:writeln('public const string ComponentNameConst = "%s";', className)
    writer:writeln()
    
    -- 生成属性
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("public override string PackageName => PackageNameConst;")
    writer:writeln()
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("public override string ComponentName => ComponentNameConst;")
    writer:writeln()
    
    -- 生成字段定义
    _writeFunctions(handler, codePkgName, classInfo)
    
    -- 写入字段定义（已经是 public）
    writer:writeln("#region UI Fields")
    writer:writeln()
    writer:writeln(defineLines)
    writer:writeln()
    writer:writeln("#endregion")
    writer:writeln()
    
    -- 生成属性绑定方法
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void BindMemberProperty()")
    writer:startBlock()
    writer:writeln("base.BindMemberProperty();")
    writer:writeln()
    writer:writeln("// 自动绑定 UI 组件字段")
    -- 逐行写入绑定代码
    for line in setVarLines:gmatch("[^\r\n]+") do
        writer:writeln(line)
    end
    writer:endBlock()
    writer:endBlock()
    writer:endBlock()
    writer:save(tempCodePath .. pathSep .. className .. ".Designer.cs")
end

--- 生成业务逻辑类文件（仅当文件不存在时）
---@param handler CS.FairyEditor.PublishHandler
---@param classInfo CS.FairyEditor.PublishHandler.ClassInfo
---@param tempCodePath string 临时代码路径
---@param codePkgName string 包名称
local function _writerLogicIfNotExists(handler, classInfo, tempCodePath, codePkgName)
    local className = classInfo.className
    local logicPath = tempCodePath .. pathSep .. className .. ".cs"
    
    -- 检查文件是否已存在
    if CS.System.IO.File.Exists(logicPath) then
        fprint(string.format("业务逻辑文件已存在，跳过生成: %s", className))
        return
    end
    
    local writer = LuaCodeWriter.new({ blockFromNewLine = true, usingTabs = false })
    writer:reset()
    
    writer:writeln("// <auto-generated>")
    writer:writeln("// This file is for business logic implementation.")
    writer:writeln("// You can safely modify this file - it will not be overwritten.")
    writer:writeln("// </auto-generated>")
    writer:writeln()
    writer:writeln("using FairyGUI;")
    writer:writeln("using TEngine;")
    writer:writeln()
    writer:writeln("namespace GameLogic")
    writer:startBlock()
    writer:writeln("/// <summary>")
    writer:writeln("/// %s 窗口业务逻辑。", className)
    writer:writeln("/// </summary>")
    writer:writeln("[WindowAttribute(UILayer.UI)]")
    writer:writeln("public partial class %s", className)
    writer:startBlock()
    
    -- 生成 override 方法实现
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void RegisterEvent()")
    writer:startBlock()
    writer:writeln("base.RegisterEvent();")
    writer:writeln("// TODO: 注册事件监听")
    writer:writeln("// 例如: BtnLogin.onClick.Add(OnClickLogin);")
    writer:endBlock()
    writer:writeln()
    
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void OnCreate()")
    writer:startBlock()
    writer:writeln("base.OnCreate();")
    writer:writeln("// TODO: 窗口创建完成后的初始化逻辑")
    writer:endBlock()
    writer:writeln()
    
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void OnRefresh()")
    writer:startBlock()
    writer:writeln("base.OnRefresh();")
    writer:writeln("// TODO: 刷新窗口数据")
    writer:endBlock()
    writer:writeln()
    
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void OnShow()")
    writer:startBlock()
    writer:writeln("base.OnShow();")
    writer:writeln("// TODO: 窗口显示时的逻辑")
    writer:endBlock()
    writer:writeln()
    
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void OnHide()")
    writer:startBlock()
    writer:writeln("base.OnHide();")
    writer:writeln("// TODO: 窗口隐藏时的逻辑")
    writer:endBlock()
    writer:writeln()
    
    writer:writeln("/// <inheritdoc/>")
    writer:writeln("protected override void OnDestroy()")
    writer:startBlock()
    writer:writeln("base.OnDestroy();")
    writer:writeln("// TODO: 窗口销毁时的清理逻辑")
    writer:endBlock()
    writer:endBlock()
    writer:endBlock()
    
    writer:save(logicPath)
    fprint(string.format("生成业务逻辑文件: %s", className))
end

local function _assertNotCrossPackage(classInfo, codePkgName)
    local members = classInfo.members
    local memberCnt = members.Count
    for j = 0, memberCnt - 1 do
        local memberInfo = members[j]
        if memberInfo.res then
            CUtil.assertCompNotCrossPackage(codePkgName,  memberInfo.res)
        end
    end
end

local function _writerView(handler, writer, tempCodePath, classInfo, codePkgName)
    local className = classInfo.className
    writer:reset()
    local members = classInfo.members
    writer:reset()

    writer:writeln("using FairyGUI;")
    writer:writeln("using FairyGUI.Utils;")
    writer:writeln()
    writer:writeln("namespace GameLogic")
    writer:startBlock()
    writer:writeln("public partial class %s : %s", classInfo.className, classInfo.superClassName)
    writer:startBlock()

    local memberCnt = members.Count
    local memberMap = {}
    local memberNameLs = {}
    for j = 0, memberCnt - 1 do
        local memberInfo = members[j]
        if memberInfo.res then
            CUtil.assertCompNotCrossPackage(codePkgName,  memberInfo.res)
        end
        if _checkIsValidMember(memberInfo.varName) then
            local fieldName = memberInfo.name
            table.insert(memberNameLs, fieldName)
            if memberMap[fieldName] then
                CUtil.error("错误", "%s 包的 %s 组件 有两个同名的字段%s", codePkgName, className, fieldName)
            end
            memberMap[fieldName] = memberInfo
        end
    end
    table.sort(memberNameLs)
    for _, fieldName in ipairs(memberNameLs) do
        local memberInfo = memberMap[fieldName]
        if _checkIsValidMember(memberInfo.varName) then
            local memberType = _getMemberType(memberInfo)
            writer:writeln("public %s %s;", memberType, _firstUpper(memberInfo.varName))
        end
    end
    writer:writeln('public const string URL = "ui://%s/%s";', handler.pkg.name, classInfo.resName)
    writer:writeln()

    writer:writeln("public static %s CreateInstance()", classInfo.className)
    writer:startBlock()
    writer:writeln('return (%s)UIPackage.CreateObject("%s", "%s");', classInfo.className, handler.pkg.name, classInfo.resName)
    writer:endBlock()
    writer:writeln()

    if handler.project.type == ProjectType.MonoGame then
        writer:writeln("protected override void OnConstruct()")
        writer:startBlock()
    else
        writer:writeln("public override void ConstructFromXML(XML xml)")
        writer:startBlock()
        writer:writeln("base.ConstructFromXML(xml);")
        writer:writeln()
    end
    for _, fieldName in ipairs(memberNameLs) do
        local memberInfo = memberMap[fieldName]
        if _checkIsValidMember(memberInfo.varName) then
            if memberInfo.group == 0 then
                local memberType = _getMemberType(memberInfo)
                writer:writeln('%s = (%s)GetChild("%s");', _firstUpper(memberInfo.varName), memberType, memberInfo.name)
            elseif memberInfo.group == 1 then
                writer:writeln('%s = GetController("%s");', _firstUpper(memberInfo.varName), memberInfo.name)
            else
                writer:writeln('%s = GetTransition("%s");', _firstUpper(memberInfo.varName), memberInfo.name)
            end

			--@FKing230630
			--FGUI，检查器，自定义数据，格式：Sound=1003,Effect=Effect_Fireworks, ...
			local customData = _getCustomData(handler, classInfo, memberInfo)
			if customData ~= "" then
				--fprint(string.format("customData:  %s  ->  %s", memberInfo.name, customData))
				local customDataProps =  _stringSplit(customData, ",")
				for i, v in pairs(customDataProps) do
					--fprint(string.format("customDataProps:  %i   ->   %s", i, v))
					local customDataItem =  _stringSplit(v, "=")
					if customDataItem ~= nil and #customDataItem == 2 then
						local customDataItemName = string.lower(customDataItem[1])--小写判断
						local customDataItemValue = string.lower(customDataItem[2])
						--fprint(string.format("customDataItem:  %s  ->  %s", customDataItemName, customDataItemValue))
						if customDataItemName == "sound" then
							writer:writeln('%s.sound = AudioManager.Instance.GetClipFGUI(%s);//CustomData_Sound', _firstUpper(memberInfo.varName), customDataItemValue)
						end
						--自定义数据拓展.....
					end
				
				end
			end
			--@FKing230630
        end
    end
    writer:writeln()
    writer:writeln("OnInit();")
    writer:endBlock()
    writer:writeln()
    writer:writeln("partial void OnInit();")
    writer:endBlock() --class
    writer:endBlock() --namepsace
    writer:save(tempCodePath .. pathSep .. "Comp" .. pathSep .. classInfo.className .. ".Designer.cs")
end


local function _copyFile(src, dest)
    local mp_log = io.open(src, "rb")
    local output = io.open(dest, "wb")
    --if not output then
    --    fprint(string.format("%s:目标目录不存在", dest))
    --end
    --if not mp_log then
    --    fprint(string.format("%s:源目录不存在", src))
    --end
    if output and mp_log then
        output:write(mp_log:read("*all"))
        output:close()
        mp_log:close()
    end
end

local function _copyTextureToProject(handler, codePkgName)
    local pkg = handler.pkg
    local texturePath = handler.exportPath .. pathSep .. ".."
    local fromPath = App.project.basePath .. pathSep .. "assets" .. pathSep .. codePkgName
    fprint(string.format("texture dst [url=file://%s]%s[/url]", texturePath, texturePath))
    fprint(string.format("texture src [url=file://%s]%s[/url]", fromPath, fromPath))
    for i = 0, pkg.items.Count - 1 do
        local item = pkg.items[i] ---@type CS.FairyEditor.FPackageItem
        if item.type ~= "folder" then
            local itemPath = string.gsub(item.path, "/", pathSep)
            local srcPath = fromPath .. itemPath .. item.fileName
            local dstPath = texturePath .. itemPath .. item.fileName
            fprint(srcPath)
            fprint(dstPath)
            _copyFile(srcPath, dstPath)
        end
    end
end

local function _execute_cmd(command)
    -- fprint(command)
    -- os.execute(command)
    --local module = [[
    --    local t = io.popen(command)
    --    local s = t:read("*all")
    --]]
    --greeter = require (module)
    local t = io.popen(command)
    local s = t:read("*all")
    fprint(s)
end

local function _writeBinder(writer, codePkgName, namespaceName, exportCodePath)
    if next(allExportedViewMap) then
        writer:reset()
        local binderName = codePkgName .. "Binder"
        writer:writeln("using FairyGUI;")
        writer:writeln()
        writer:writeln("namespace %s", namespaceName)
        writer:startBlock()
        writer:writeln("public class %s", binderName)
        writer:startBlock()

        writer:writeln("public static void BindAll()")
        writer:startBlock()
        if allExportedViewMap then
            local lsClass = {}
            for className, classInfo in pairs(allExportedViewMap) do
                table.insert(lsClass, className)
            end
            table.sort(lsClass)
            for i, className in ipairs(lsClass) do
                local classInfo = allExportedViewMap[className]
                writer:writeln("UIObjectFactory.SetPackageItemExtension(%s.URL, typeof(%s));",
                        classInfo.className, classInfo.className)
            end
        end
        writer:endBlock() --bindall
        writer:endBlock() --class
        writer:endBlock() --namespace
        writer:save(exportCodePath .. "/" .. binderName .. ".cs")
    end
end

-- local copyTextureLs = {"CommonTex"}
---@param handler CS.FairyEditor.PublishHandler
local function genCode(handler)
    local settings = handler.project:GetSettings("Publish").codeGeneration
    local codePkgName = handler:ToFilename(handler.pkg.name) --convert chinese to pinyin, remove special chars etc.
    fprint(codePkgName)
    local exportCodePath = handler.exportCodePath .. pathSep .. codePkgName
    local namespaceName = codePkgName
    if settings.packageName ~= nil and settings.packageName ~= "" then
        namespaceName = settings.packageName .. "." .. namespaceName
    end
    local tempCodePath = App.project.basePath .. pathSep .. "tempCodes" .. pathSep .. codePkgName
    if App.isMacOS then
        CS.FairyEditor.ProcessUtil.Start("/bin/rm", {"-rf", tempCodePath}, nil, true)
    else
        _execute_cmd('rmdir /s /q ' .. tempCodePath)
    end
    fprint(string.format("exportCodePath:[url=file://%s]%s[/url]", exportCodePath, exportCodePath))
    fprint(string.format("tempCodePath:[url=file://%s]%s[/url]", tempCodePath, tempCodePath))
    fprint(string.format("codePkgName:%s", codePkgName))

    settings.ignoreNoname = false
    local classes = handler:CollectClasses(settings.ignoreNoname, settings.ignoreNoname, nil)
    handler:SetupCodeFolder(tempCodePath, "cs")
    handler:SetupCodeFolder(tempCodePath .. pathSep .. "Comp", "cs")

    -- if _inLs(copyTextureLs, codePkgName) then
    --     _copyTextureToProject(handler,codePkgName)
    --     fprint("拷贝贴图完成")
    --     handler.exportPath = App.project.basePath .. pathSep .. "tempTrash"
    --     return
    -- end

    allListRenderMap = {}
    allExportedViewMap = {}
    resMap = {}
    local items = handler.items
    for i = 0, items.Count - 1 do
        local res = items[i]
        resMap[res.name] = res
    end
    CUtil.assertNoComponentSame(handler)
    local writer = LuaCodeWriter.new({ blockFromNewLine = true, usingTabs = false })-- CodeWriter.new({fileMark=""})
    for i = 0, classes.Count - 1 do
        local classInfo = classes[i]
        local isExported = classInfo.res.exported
        _assertNotCrossPackage(classInfo, codePkgName)
        if isExported then
            if CUtil.isWindow(classInfo.className) then
                _writerWindow(handler, writer, codePkgName, tempCodePath, classInfo)
            else
                _writerView(handler, writer, tempCodePath, classInfo, codePkgName)
                allExportedViewMap[classInfo.className] = classInfo
            end
            CUtil.assertClassStartWithUpper(classInfo.className)
        end
    end
    _writeBinder(writer, codePkgName, "GameLogic", tempCodePath)
    --_copyTextureToProject(handler,codePkgName)
end

return genCode
