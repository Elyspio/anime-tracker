import { useEffect } from "react";

const product = "Anime Tracker";

/** Names the tab after what is on screen, so two of them can be told apart. */
export function useDocumentTitle(title: string | null): void {
	useEffect(() => {
		document.title = title ? `${title} | ${product}` : product;

		return () => {
			document.title = product;
		};
	}, [title]);
}
