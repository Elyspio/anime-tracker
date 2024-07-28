import { createSlice } from "@reduxjs/toolkit";
import { getAnimes } from "./animes.actions";
import { Anime } from "@apis/backend/generated";

export type AnimeState = {
	data: Anime[];
};

const initialState: AnimeState = {
	data: [],
};

const slice = createSlice({
	name: "todo",
	initialState,
	reducers: {},
	extraReducers: (builder) => {
		builder.addCase(getAnimes.fulfilled, (state, action) => {
			state.data = action.payload;
		});
	},
});

export const animesReducer = slice.reducer;
