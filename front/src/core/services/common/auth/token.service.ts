import { inject, injectable } from "inversify";
import { DiKeysService } from "@/core/di/services/di.keys.service";
import { LocalStorageService } from "../localStorage.service";
import { User } from "@apis/authentication/generated";

@injectable()
export class TokenService {
	@inject(DiKeysService.localStorage.jwt)
	private localStorage!: LocalStorageService;

	public parseJwt(token: string): User {
		const base64Url = token.split(".")[1];
		const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
		const jsonPayload = decodeURIComponent(
			window
				.atob(base64)
				.split("")
				.map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
				.join(""),
		);

		return JSON.parse(jsonPayload).data;
	}

	public getToken() {
		return this.localStorage.get<string>();
	}

	public delete() {
		this.localStorage.remove();
	}
}
