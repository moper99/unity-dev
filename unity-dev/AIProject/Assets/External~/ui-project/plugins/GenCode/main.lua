require(PluginPath..'/CUtil')
require(PluginPath..'/RegexUtil')
local genCode = require(PluginPath..'/GenCodeLua')

local AOTName="LauncherUI"

local skipCodePkgMap = {
    ["Textures"] = true,
}

---@param handler CS.FairyEditor.PublishHandler
function onPublish(handler)
    App.consoleView:Clear()
    local pkgName = handler.pkg.name
    fprint("生成代码开始:".. pkgName)
    if skipCodePkgMap[pkgName] then
        fprint(string.format("%s 已跳过代码生成",pkgName))
        return
    end
    if pkgName == AOTName then
        --handler.CODE_FILE_MARK = ""
        handler:CollectClasses(true, true, AOTName)
        handler:SetupCodeFolder(handler.exportCodePath, "cs", "")
        handler.genCode = true
        fprint(AOTName.."已生成代码")
        return
    end
    handler.genCode = false -- 为了屏蔽默认生成CS代码
    local isOK = xpcall(genCode, function(errMsg)
        fprint(errMsg .. "\n".. debug.traceback())
    end, handler)
    if not isOK then
        handler.paused = true
    end
    fprint("生成代码结束...")

    
end

local appActive = true
function OnUpdateCheckAppActive(obj)
    -- if App.isActive ~= appActive then
    --     appActive = App.isActive
    --     if appActive then
    --         --自动刷新所有项目
    --         App.RefreshProject()
    --     else
    --         --自动保存所有文档
    --         -- App.docView:SaveAllDocuments()
    --     end
    -- end
end

CS.FairyGUI.Timers.inst:AddUpdate(OnUpdateCheckAppActive)

function ExportLocalizationText()
    -- 创建 ExportStringsHandler 实例
    local handler = CS.FairyEditor.ExportStringsHandler()
    
    --创建一个CS.System.Collections.Generic.List 实例，类型为CS.FairyEditor.FPackage
    local packages = CS.System.Collections.Generic.List(typeof(CS.FairyEditor.FPackage))()
    for i = 0, CS.FairyEditor.App.project.allPackages.Count - 1 do
        local package = CS.FairyEditor.App.project.allPackages[i]
        if package.name ~= AOTName then
            packages:Add(package)
        end
    end

    -- 解析项目包
    handler:Parse(packages, true, false)
    
    --文件导出路径
    local exportPath = CS.FairyEditor.App.project.basePath .. "/../../GameRes/Configs/Localization/test.xml"
    -- 导出字符串到 XML 文件
    handler:Export(exportPath, false)

    CS.FairyEditor.App.Alert("导出多语言文件成功")
    fprint("导出多语言文件成功:"..exportPath)
end

local toolMenu = App.menu:GetSubMenu("tool");
toolMenu:AddItem("导出多语言文本", "ExportLocalizationText", function(menuItem)
    ExportLocalizationText();
end)

-------do cleanup here-------

function onDestroy()
    CS.FairyGUI.Timers.inst:Remove(OnUpdateCheckAppActive)
    toolMenu:RemoveItem("ExportLocalizationText")
end



