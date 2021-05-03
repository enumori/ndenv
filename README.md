# ndenv
Simple Node.JS version management

# 1. Installation
レポジトリのソリューションのビルドを行い、[リリースフォルダーに作成されたファイル](https://github.com/enumori/ndenv/releases/download/2021.05.03/ndenv.zip)を任意のフォルダーに配置します。配置したフォルダーにパスを通します。

もしくは、powershellを起動して以下のコマンドを入力します。

```
Set-ExecutionPolicy RemoteSigned -scope Process
Invoke-WebRequest -Uri "https://github.com/enumori/ndenv/releases/download/2021.05.03/ndenv.zip" -OutFile .\ndenv.zip
Expand-Archive -Path .\ndenv.zip -DestinationPath $env:USERPROFILE
Remove-Item .\ndenv.zip
Rename-Item  $env:USERPROFILE\ndenv  $env:USERPROFILE\.ndenv
$path = [Environment]::GetEnvironmentVariable("PATH", "User")
$path = "$env:USERPROFILE\.ndenv;" + $path
[Environment]::SetEnvironmentVariable("PATH", $path, "User")
```
powershellやコマンドプロンプトを起動するとndenvが使用できます。

# 2. Command Reference
| 実行内容 | コマンド|
| --- | --- |
| インストール可能なNode.JSバージョンのリスト | ndenv install --list |
| インストール | ndenv install バージョン |
| インストール済みバージョンのリスト | ndenv versions |
| 全体のバージョンの切り替え | ndenv global バージョン |
| ローカルフォルダーのバージョンの切り替え | ndenv local バージョン |

# 3. 使い方
## 3.1. Node.JSをダウンロードする
```
PS > ndenv install v12.16.2
```
## 3.2. 使用するバージョンに設定する
```
PS > ndenv global v12.16.2
```
## 3.3. 指定したバージョンが使用できるかを確認
```
PS > node --version
2で設定したバージョンが表示される
```