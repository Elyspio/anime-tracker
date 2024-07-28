import { inject, injectable } from "inversify";
import { BackendApi } from "@apis/backend";

@injectable()
export class AnimeService {
	@inject(BackendApi)
	private backendApiClient!: BackendApi;

	public getAnimes() {
		return this.backendApiClient.animes.getAnimes();
	}
}
