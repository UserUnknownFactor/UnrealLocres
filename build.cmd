IF NOT DEFINED IS_ENV_READY (
    SET IS_ENV_READY=1
    CALL "%VSINSTALLDIR%\VC\Auxiliary\Build\vcvars64.bat"
)
msbuild UnrealLocres.sln /p:Configuration=Debug /p:Platform="Any CPU"