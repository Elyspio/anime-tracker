import { BackendApi } from "@apis/backend";
import { AuthenticationApiClient } from "@apis/authentication";
import { Container } from "inversify";
import { JwtClient } from "@apis/authentication/generated";

export const addApis = (container: Container) => {
	container.bind(BackendApi).toSelf();
	container.bind<AuthenticationApiClient>(AuthenticationApiClient).toSelf();
	container.bind<JwtClient>(JwtClient).toSelf();
};
