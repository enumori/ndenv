@ECHO OFF
SETLOCAL
IF NOT DEFINED LOCAL_NDENV_VER_FILE (
  SET LOCAL_NDENV_VER_FILE="%CD%\.node-version"
)
IF NOT DEFINED GLOBAL_NDENV_VER_FILE (
  SET GLOBAL_NDENV_VER_FILE="%~dp0.node-version"
)
IF EXIST %LOCAL_NDENV_VER_FILE% (
  FOR /F "USEBACKQ" %%A IN (%LOCAL_NDENV_VER_FILE%) DO (
    IF NOT DEFINED NDENV_NODE_DIR (
      SET NDENV_NODE_DIR="%~dp0\versions\%%A\\"
    )
  )
) ELSE IF EXIST %GLOBAL_NDENV_VER_FILE% (
  FOR /F "USEBACKQ" %%A IN (%GLOBAL_NDENV_VER_FILE%) DO (
    IF NOT DEFINED NDENV_NODE_DIR (
      SET NDENV_NODE_DIR="%~dp0\versions\%%A\\"
    )
  )
) ELSE (
  ECHO "ndenv global もしくはndenv local コマンドで使用するNode.Jsのバージョンを指定してください。"
  EXIT /B
)

IF EXIST "%NDENV_NODE_DIR%yarnpkg.cmd" (
  CALL "%NDENV_NODE_DIR%yarnpkg.cmd" %*
) ELSE (
  ECHO "yarnpkg.cmdがみつかりません。"
)
ENDLOCAL