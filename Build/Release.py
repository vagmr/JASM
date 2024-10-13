import os
import shutil
import re
import sys


ELEVATOR_CSPROJ = "src\\Elevator\\Elevator.csproj"
ELEVATOR_OUTPUT_FILE = "src\\Elevator\\bin\\Release\\Publish\\Elevator.exe"

JASM_CSPROJ = "src\\GIMI-ModManager.WinUI\\GIMI-ModManager.WinUI.csproj"
JASM_OUTPUT = "src\\GIMI-ModManager.WinUI\\bin\\Release\Publish\\"

JASM_Updater_CSPROJ = "src\\JASM.AutoUpdater\\JASM.AutoUpdater.csproj"
JASM_Updater_OUTPUT = "src\\\\JASM.AutoUpdater\\bin\\Release\Publish\\"
JASM_Updater_FolderName = "JASM - Auto Updater_New"

RELEASE_DIR = "output"
JASM_RELEASE_DIR = "output\\JASM"

SelfContained = "SelfContained" in sys.argv
ExcludeElevator = "ExcludeElevator" in sys.argv

def checkSuccessfulExitCode(exitCode: int) -> None:
	if exitCode != 0:
		print("Exit code: " + str(exitCode))
		exit(exitCode)

def extractVersionNumber() -> str:
	with open(JASM_CSPROJ, "r") as jasmCSPROJ:
		for line in jasmCSPROJ:
			line = line.strip()
			if line.startswith("<VersionPrefix>"):
				return re.findall("\d+\.\d+\.\d+", line)

print("Release.py")
print("PWD: " + os.getcwd())
print("SelfContained: " + str(SelfContained))

versionNumber = extractVersionNumber()
if versionNumber is None or len(versionNumber) == 0:
	print("Failed to extract version number from " + JASM_CSPROJ)
	exit(1)
versionNumber = versionNumber[0]

# 构建函数
def build_project(project_name, csproj_path, output_path, self_contained=False):
	print(f"Building {project_name}...")
	publish_profile = "FolderProfileSelfContained.pubxml" if self_contained else "FolderProfile.pubxml"
	publish_command = f"dotnet publish {csproj_path} /p:PublishProfile={publish_profile} -c Release"
	print(publish_command)
	checkSuccessfulExitCode(os.system(publish_command))
	print(f"Finished building {project_name}")
	return output_path

# 构建 Elevator (如果需要)
if not ExcludeElevator:
	build_project("Elevator", ELEVATOR_CSPROJ, ELEVATOR_OUTPUT_FILE)

# 构建 JASM Auto Updater (仅非自包含版本)
if not SelfContained:
	build_project("JASM - Auto Updater", JASM_Updater_CSPROJ, JASM_Updater_OUTPUT)

# 构建 JASM (自包含和非自包含版本)
jasm_output = build_project("JASM", JASM_CSPROJ, JASM_OUTPUT, SelfContained)

# 创建发布目录
os.makedirs(RELEASE_DIR, exist_ok=True)
os.makedirs(JASM_RELEASE_DIR, exist_ok=True)

# 复制文件到发布目录
if not ExcludeElevator:
	print("Copying Elevator to JASM...")
	checkSuccessfulExitCode(os.system(f"copy {ELEVATOR_OUTPUT_FILE} {JASM_RELEASE_DIR}"))

print("Copying JASM to output...")
shutil.copytree(jasm_output, JASM_RELEASE_DIR, dirs_exist_ok=True)

if not SelfContained:
	print("Copying JASM - Auto Updater to output...")
	os.makedirs(os.path.join(JASM_RELEASE_DIR, JASM_Updater_FolderName), exist_ok=True)
	shutil.copytree(JASM_Updater_OUTPUT, os.path.join(JASM_RELEASE_DIR, JASM_Updater_FolderName), dirs_exist_ok=True)

print("Copying text files to RELEASE_DIR...")
shutil.copy("Build\\README.txt", RELEASE_DIR)
shutil.copy("CHANGELOG.md", os.path.join(RELEASE_DIR, "CHANGELOG.txt"))

# 压缩发布目录
print("Zipping release directory...")
releaseArchiveName = f"JASM_v{versionNumber}_{'SelfContained' if SelfContained else 'Regular'}.7z"
checkSuccessfulExitCode(os.system(f"7z a -mx4 {releaseArchiveName} .\\{RELEASE_DIR}\\*"))

# 设置 GitHub Actions 环境变量
env_file = os.getenv('GITHUB_ENV')
if env_file:
	with open(env_file, "a") as myfile:
		myfile.write(f"zipFile={releaseArchiveName}")

checkSuccessfulExitCode(os.system(f"7z h -scrcsha256 .\\{releaseArchiveName}"))

exit(0)
