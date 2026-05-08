
CUtil = {}

function CUtil.error(title, format, ...)
    local errString = string.format(format, ...)
    local err = CS.System.Exception(errString)
    CS.FairyEditor.App.Alert(title, err, function ()
    end)
    fprint("发布失败:" .. errString)
end

function CUtil.isWindow(name)
    if string.match(name, ".*Panel$") then
        return true
    end
    return false
end

function CUtil.assertCompNotCrossPackage(rawPackageName, packageItem)
    local packageName = packageItem.owner and packageItem.owner.name
    if packageName ~= rawPackageName and packageName ~= "Basic" then
        CUtil.error("组件引用错误，", "组件%s 被跨包引用", packageItem.name)
    end
end

-- 类型的要求
function CUtil.assertClassStartWithUpper(name)
    if not RegexUtil.isMatch(name, "^[A-Z][A-Za-z0-9]+") then
        CUtil.error("组件名称错误", "%s 不是合法的类名(骆驼命名)", name)
    end
end


function CUtil.isMatch(className, patStr)
    return RegexUtil.isMatch(className, patStr)
end

---@param handler CS.FairyEditor.PublishHandler
function CUtil.assertNoComponentSame(handler)
    local ls = handler.project.allPackages
    local curExportName = handler:ToFilename(handler.pkg.name)
    local exportedComponentMap = {}
    local projectResMap = {}
    for i=0,ls.Count - 1 do
        local pkg = ls[i]
        local items = pkg.items
        local pkgName = pkg.name
        projectResMap[pkgName] = projectResMap[pkgName] or {}
        for j=0,items.Count - 1 do
            local item = items[j]
            if item.type == "component" then
                if (pkgName == "Common01" or pkgName == "Common02") then
                    if curExportName == pkgName then
                        CUtil.error(pkgName, "%s里不要有组件(%s)", pkgName, item.name)
                    end
                end
                if item.exported then
                    local oldPkg = exportedComponentMap[item.name]
                    if oldPkg then
                        if curExportName == oldPkg or curExportName == pkgName then
                            CUtil.error("重名", "%s 包 和 %s 包 的组件 %s 同名了", oldPkg, pkgName, item.name)
                        end
                    end
                    exportedComponentMap[item.name] = pkgName
                end
            else
                --if pkgName == "CommonWidget" and item.type ~= "folder" then
                --    if curExportName == pkgName then
                --        CUtil.error(pkgName, "CommonWidget里不要有组件以外的资源(%s)", item.name)
                --    end
                --elseif pkgName == "CommonBtns" and item.type ~= "folder" then
                --    if curExportName == pkgName then
                --        CUtil.error(pkgName, "CommonBtns 里不要有组件以外的资源(%s)", item.name)
                --    end
                --end

            end
            if pkgName == "CommonTex" then
                if item.type ~= "image" and item.type ~= "folder" then
                    CUtil.error(pkgName, "CommonTex里不要有图片以外的资源(%s)", item.name)
                end
            end
            --projectResMap[pkgName][item.name] = item
            --if item.type == "image" and (item.width >= 300 and item.height >= 300) and item.exported then
            --    if bigImageSkipCheckPkg[pkgName] == nil then
            --        if curExportName == pkgName then
            --            CUtil.error(pkgName, "%s里有超大的图片%s,一般贴图请放在CommonTex,龙骨贴图请放在DragonBones里", pkgName, item.name)
            --        end
            --    end
            --end
        end
    end
    return projectResMap
end
