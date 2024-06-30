import { inject, injectable } from "inversify";
import axios from "axios";
import { AuthenticationClient, JwtClient } from "./generated";
import { TokenService } from "@services/common/auth/token.service";

@injectable()
export class AuthenticationApiClient {
	public readonly auth: AuthenticationClient;
	public readonly jwt: JwtClient;

	constructor(@inject(TokenService) tokenService: TokenService) {
		const instance = axios.create({ withCredentials: true, transformResponse: [] });

		instance.interceptors.request.use((value) => {
			value.headers!["Authorization"] = `Bearer ${tokenService.getToken()}`;
			return value;
		});

		this.auth = new AuthenticationClient(window.config.endpoints.authentication, instance);
		this.jwt = new JwtClient(window.config.endpoints.authentication, instance);
	}
}
