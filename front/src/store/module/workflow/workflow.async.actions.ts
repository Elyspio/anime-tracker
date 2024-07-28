import { createAsyncActionGenerator } from "../../common/common.actions";
import { silentLogin } from "../authentication/authentication.async.action";
import { getAnimes } from "@store/module/animes/animes.actions";

const createAsyncThunk = createAsyncActionGenerator("workflow");

export const initApp = createAsyncThunk("initApp", (_, { dispatch }) => {
	dispatch(silentLogin());
	dispatch(getAnimes());
});
