import { AuthenticationService } from "@services/common/auth/authentication.service";
import { ThemeService } from "@services/common/theme.service";
import { LocalStorageService } from "@services/common/localStorage.service";
import { DiKeysService } from "./di.keys.service";
import { AnimeService } from "@services/anime.service";
import { Container } from "inversify";
import { TokenService } from "@services/common/auth/token.service";

export const addServices = (container: Container) => {
	container.bind(AuthenticationService).toSelf();
	container.bind(TokenService).toSelf();
	container.bind(AnimeService).toSelf();
	container.bind(ThemeService).toSelf();
	container.bind<LocalStorageService>(DiKeysService.localStorage.jwt).toConstantValue(new LocalStorageService("authentication:jwt"));
};
