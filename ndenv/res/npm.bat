@ECHO OFF
SETLOCAL
SET LOCAL_VER_FILE="%CD%\.node-version"
SET GLOBAL_VER_FILE="%~dp0.node-version"
IF EXIST %LOCAL_VER_FILE% (
    FOR /F "USEBACKQ" %%A IN (%LOCAL_VER_FILE%) DO (
        CALL "%~dp0\versions\%%A\npm.cmd" %*
    )
) ELSE (
    FOR /F "USEBACKQ" %%A IN (%GLOBAL_VER_FILE%) DO (
        CALL "%~dp0\versions\%%A\npm.cmd" %*
    )
)
ENDLOCAL
