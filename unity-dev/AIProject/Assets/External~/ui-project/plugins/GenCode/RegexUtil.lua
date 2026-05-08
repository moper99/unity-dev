RegexUtil = {}

local Regex = CS.System.Text.RegularExpressions.Regex

function RegexUtil.isMatch(str, patStr)
    return Regex.IsMatch(str, patStr)
end

function RegexUtil.capitalize(str)
    return (str:gsub("^%l", string.upper))
end

function RegexUtil.firstToUpper(s)
    local head = string.upper(Regex.Replace(s, "([a-zA-Z])([a-zA-Z0-9_]+)", "$1"))
    local tail = Regex.Replace(s, "([a-zA-Z])([a-zA-Z0-9_]+)", "$2")
    return head .. tail
end


