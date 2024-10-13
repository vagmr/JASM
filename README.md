# JASM - 又一个皮肤管理器 (汉化版)

这是JASM(Just Another Skin Manager)的汉化版本。JASM是一个使用WinUI 3和WinAppSDK开发的游戏皮肤管理器。

> 原项目地址: [https://github.com/Jorixon/JASM](https://github.com/Jorixon/JASM)

## 主要功能

- 美观的用户界面
- 支持直接拖放文件到应用中
- 自动将未分类的mod整理到相应角色文件夹
- 在角色间移动mod
- 直接从应用启动3Dmigto启动器和游戏
- 监控角色文件夹,自动更新添加或删除的皮肤
- 编辑merged.ini键值
- 将JASM管理的所有mod导出(复制)到指定文件夹
- 使用F10或应用内刷新按钮刷新mod(需要提权的辅助进程,见下文说明)

## 快捷键

- "空格键" - 在角色视图中,切换选中mod的开启/关闭状态
- "F10" - 如果提权进程和游戏正在运行,刷新游戏中的mod
- "F5" - 在角色视图中,从磁盘刷新该角色的mod
- "CTRL + F" - 在角色概览中,聚焦到搜索栏
- "ESC" - 在角色视图中,返回角色概览
- "F1" - 在角色视图中,打开可选择的游戏内皮肤

## 系统要求

- Windows 10 1809或更高版本
- [.NET Desktop Runtime](https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&rid=win10-x64&apphost_version=8.0.0&gui=true)
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)

如果缺少这些依赖,应用会提示您下载必要组件并提供链接。

## 已知问题

### 内存占用高

每次页面导航都会分配大量内存且不释放。这导致快速在页面间切换时,应用内存使用量迅速超过1GB。这不是一个简单的修复。如果您发现应用变慢,建议重启应用。

根据研究,WinUI在页面导航时可能存在内存泄漏。目前不确定是否为WinUI问题或代码问题。大部分是非托管内存,内存分析器帮助不大。

### Elevator

程序可能被标记为恶意软件,您需要从[Releases页面](https://github.com/Jorixon/JASM/releases/tag/v2.14.3)手动下载。

## 注意事项

- 这仍处于早期开发阶段。请自行备份并谨慎使用 ⚠️
- 未处理的异常会写入日志文件。可在appsettings.json中启用调试日志
- 应用中可能存在一些"GIMI-ModManager"引用,这是项目的原始名称。将来会进行更改
