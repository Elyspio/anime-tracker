import { createAsyncActionGenerator } from "../../common/common.actions";
import { silentLogin } from "../authentication/authentication.async.action";

const createAsyncThunk = createAsyncActionGenerator("workflow");

export const initApp = createAsyncThunk("initApp", (_, { dispatch }) => {
	dispatch(silentLogin());
});
