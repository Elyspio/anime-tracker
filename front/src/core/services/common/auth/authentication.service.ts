import { inject, injectable } from "inversify";
import { AuthenticationApiClient } from "@apis/authentication";
import { EventManager } from "@/core/utils/event";
import { User } from "@apis/authentication/generated";
import { LocalStorageService } from "../localStorage.service";
import { openPage } from "@/core/utils/web";
import { DiKeysService } from "@/core/di/services/di.keys.service";

@injectable()
export class AuthenticationService {
	constructor(
		@inject(DiKeysService.localStorage.jwt) private localStorage: LocalStorageService,
		@inject(AuthenticationApiClient) private authenticationApi: AuthenticationApiClient,
	) {}

	public openLoginPage() {
		return openPage(`${window.config.endpoints.authentication}/login`);
	}

	public async logout() {
		await this.localStorage.remove();
		AuthenticationEvents.emit("logout");
	}

	public isValid() {
		return this.authenticationApi.jwt.verify();
	}
}

export const AuthenticationEvents = new EventManager<{
	login: (user: User) => void;
	logout: () => void;
}>();
