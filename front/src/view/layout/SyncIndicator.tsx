import { Box, ButtonBase, Stack, Tooltip } from "@mui/material";
import { formatInstant, isRunActive } from "@/core/refreshRuns";
import { Eyebrow } from "@/view/components/Eyebrow";
import type { RefreshRun } from "@/core/api/types";

interface Props {
	runs: RefreshRun[] | undefined;
	onOpen: () => void;
}

/**
 * When the data was last known good, and the way into the record of every run. Anonymous readers
 * see it too: an empty grid is explained by whether a refresh has ever succeeded.
 */
export function SyncIndicator({ runs, onOpen }: Props) {
	const active = (runs ?? []).some(isRunActive);
	const lastFinished = (runs ?? [])
		.filter((run) => run.status === "Succeeded" && run.finishedAt !== null)
		.map((run) => run.finishedAt as string)
		.sort()
		.at(-1);

	const label = active ? "Refreshing" : lastFinished ? formatInstant(lastFinished) : "Never";

	return (
		<Tooltip title="Refreshes">
			<ButtonBase
				onClick={onOpen}
				sx={{ borderRadius: 1, px: 1, py: 0.5, "&:hover": { bgcolor: "action.hover" } }}
			>
				<Stack direction="row" sx={{ alignItems: "center", gap: 1 }}>
					{active && (
						<Box
							sx={{
								width: 6,
								height: 6,
								borderRadius: "50%",
								// Clay, not green: success green means "bingeable" and nothing else,
								// which is the same reason a finished run gets a neutral chip.
								bgcolor: "info.main",
								// The system's one animation, and it is spent on live data.
								animation: "pulse 2s ease-in-out infinite",
							}}
						/>
					)}
					<Eyebrow>Synced {label}</Eyebrow>
				</Stack>
			</ButtonBase>
		</Tooltip>
	);
}
