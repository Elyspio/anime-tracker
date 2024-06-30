import { inject, injectable } from "inversify";
import { TodoClient, TodoUserClient } from "./generated";
import { TokenService } from "@services/common/auth/token.service";
import axios from "axios";

@injectable()
export class BackendApi {
	public todo: { common: TodoClient; user: TodoUserClient };

	constructor(@inject(TokenService) tokenService: TokenService) {
		const instance = axios.create({ withCredentials: true, transformResponse: [] });

		instance.interceptors.request.use((value) => {
			value.headers!["Authorization"] = `Bearer ${tokenService.getToken()}`;
			return value;
		});

		this.todo = {
			common: new TodoClient(window.config.endpoints.core, instance),
			user: new TodoUserClient(window.config.endpoints.core, instance),
		};
	}
}
