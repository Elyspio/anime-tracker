import { createActionGenerator } from "../../common/common.actions";

const createAction = createActionGenerator("theme");

export const setTheme = createAction<"dark" | "light">("set");
export const toggleTheme = createAction("toggle");
