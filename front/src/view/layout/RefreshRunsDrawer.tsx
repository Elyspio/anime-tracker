import { Alert, Box, Chip, Divider, Drawer, LinearProgress, Stack, Typography } from "@mui/material";
import { formatDuration, formatInstant, groupPasses, isRunActive, kindLabels, statusLabels, statusTones, type RefreshPass } from "@/core/refreshRuns";
import { seasonLabels } from "@/core/binge";
import { Eyebrow } from "@/view/components/Eyebrow";
import { Mono } from "@/view/components/Mono";
import type { RefreshRun } from "@/core/api/types";

interface Props {
	open: boolean;
	onClose: () => void;
	runs: RefreshRun[] | undefined;
	isPending: boolean;
	error: Error | null;
}

/** Kind, count, duration, status: one column each, so a pass's two halves read down as well as across. */
const rowColumns = "84px 1fr auto auto";

function RunRow({ run }: { run: RefreshRun }) {
	const active = isRunActive(run);
	const count = run.kind === "Dub" ? `${run.total} matched${active ? " so far" : ""}` : `${run.total} anime${run.total === 1 ? "" : "s"}`;

	return (
		<Box sx={{ display: "grid", gridTemplateColumns: rowColumns, columnGap: 1.5, rowGap: 0.75, alignItems: "center" }}>
			<Eyebrow>{kindLabels[run.kind]}</Eyebrow>
			<Mono sx={{ color: "text.secondary" }}>{count}</Mono>
			<Mono sx={{ color: "text.disabled" }}>{formatDuration(run)}</Mono>
			<Chip size="small" label={statusLabels[run.status]} color={statusTones[run.status]} variant={statusTones[run.status] === "default" ? "outlined" : "filled"} />

			{active && <LinearProgress sx={{ gridColumn: "1 / -1" }} />}

			{run.error && (
				<Typography variant="caption" sx={{ gridColumn: "1 / -1", color: "error.main" }}>
					{run.error}
				</Typography>
			)}
		</Box>
	);
}

/**
 * A pass is what one click — or one night — sets off: the season fetch, then the dub matching it
 * queues. Listing them as one event halves the drawer and keeps a dub count next to the season it
 * was measured against.
 */
function PassCard({ pass }: { pass: RefreshPass }) {
	return (
		<Stack spacing={1.25}>
			<Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "baseline", gap: 1 }}>
				<Typography variant="subtitle1">
					{seasonLabels[pass.date.season]} {pass.date.year}
				</Typography>
				<Mono sx={{ color: "text.disabled" }}>{formatInstant(pass.startedAt)}</Mono>
			</Stack>

			{pass.season && <RunRow run={pass.season} />}
			{pass.dub && <RunRow run={pass.dub} />}
		</Stack>
	);
}

export function RefreshRunsDrawer({ open, onClose, runs, isPending, error }: Props) {
	const passes = runs ? groupPasses(runs) : [];
	const activeCount = runs?.filter(isRunActive).length ?? 0;

	return (
		<Drawer anchor="right" open={open} onClose={onClose}>
			<Box sx={{ width: { xs: 340, sm: 440 }, p: 3 }}>
				<Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "baseline", mb: 0.5 }}>
					<Typography variant="h5">Refreshes</Typography>
					{activeCount > 0 && <Eyebrow sx={{ color: "info.main" }}>{activeCount} running</Eyebrow>}
				</Stack>
				<Typography variant="body2" sx={{ color: "text.secondary", mb: 2.5 }}>
					Each pass fetches the season from AniList, then matches its French dubs.
				</Typography>

				{isPending && <LinearProgress />}

				{error && <Alert severity="error">Could not load refreshes: {error.message}</Alert>}

				{runs && runs.length === 0 && <Alert severity="info">No refresh recorded yet.</Alert>}

				<Stack divider={<Divider />} spacing={2.5}>
					{passes.map((pass) => (
						<PassCard key={pass.key} pass={pass} />
					))}
				</Stack>
			</Box>
		</Drawer>
	);
}
