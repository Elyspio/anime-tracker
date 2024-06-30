import * as React from "react";

export const useAsyncEffect = (func: () => Promise<void>, dependencies: any[]) => {
	React.useEffect(() => {
		(async () => {
			await func();
		})();
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, dependencies);
};
