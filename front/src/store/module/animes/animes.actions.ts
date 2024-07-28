import { ExtraArgument } from "@store";
import { AnimeService } from "@services/anime.service";
import { createAsyncActionGenerator } from "../../common/common.actions";

const createAsyncThunk = createAsyncActionGenerator("animes");

export const getAnimes = createAsyncThunk("getTodo", async (_, { extra }) => {
	const { container } = extra as ExtraArgument;
	const service = container.get(AnimeService);
	return service.getAnimes();
});
