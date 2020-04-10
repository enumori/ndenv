# ndenv
Simple Node.JS version management

# Installation
powershellを起動して以下のコマンドを入力します。
```
Set-ExecutionPolicy RemoteSigned -scope Process
Invoke-WebRequest -Uri "https://github.com/enumori/ndenv/releases/download/2020.4.10/ndenv.zip" -OutFile .\ndenv.zip
Expand-Archive -Path .\ndenv.zip -DestinationPath $env:USERPROFILE
Remove-Item .\ndenv.zip
Rename-Item  $env:USERPROFILE\ndenv  $env:USERPROFILE\.ndenv
$path = [Environment]::GetEnvironmentVariable("PATH", "User")
$path = "$env:USERPROFILE\.ndenv;" + $path
[Environment]::SetEnvironmentVariable("PATH", $path, "User")
```

# Command Reference
| 実行内容 | コマンド|
| --- | --- |
| インストール可能なPythonバージョンのリスト | ndenv install --list |
| インストール | ndenv install バージョン |
| インストール済みバージョンのリスト | ndenv versions |
| 全体のバージョンの切り替え | ndenv global バージョン |
| ローカルフォルダーのバージョンの切り替え | ndenv local バージョン |
