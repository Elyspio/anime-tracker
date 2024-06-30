variable "GITHUB_TOKEN" {
	default = "$GITHUB_TOKEN"
}

target "default" {
	context    = "../.."
	dockerfile = "./deploy/build/dockerfile"
	platforms  = [
		"linux/amd64",
#		"linux/arm64"
	]
	tags = [
		"elyspio/react-api-template:latest"
	]
	args = {
		SLN_PATH         = "ExampleApi.sln"
		MAIN_CSPROJ_PATH = "Web/Example.Api.Web.csproj"
		ROOT_FOLDER      = "back/"
		ENTRY_DLL        = "Example.Api.Web.dll"
		GITHUB_TOKEN     = "${GITHUB_TOKEN}"
	}

}