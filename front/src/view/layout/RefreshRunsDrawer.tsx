import {
	Alert,
	Box,
	Chip,
	Divider,
	Drawer,
	LinearProgress,
	Stack,
	Typography,
} from "@mui/material";
import {
	formatDuration,
	formatInstant,
	isRunActive,
	statusLabels,
	statusTones,
} from "@/core/refreshRuns";
import { seasonLabels } from "@/core/binge";
import { Mono } from "@/view/components/Mono";
import type { RefreshRun } from "@/core/api/types";

interface Props {
	open: boolean;
	onClose: () => void;
	runs: RefreshRun[] | undefined;
	isPending: boolean;
	error: Error | null;
}

/**
 * A refresh is one fetch and lasts about a second, so there is no progress to bar — what a job
 * running unattended every night leaves behind is a record: when, how many, and what stopped it.
 */
function RunCard({ run }: { run: RefreshRun }) {
	const active = isRunActive(run);

	return (
		<Stack spacing={1}>
			<Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center" }}>
				<Typography variant="subtitle2">
					{seasonLabels[run.date.season]} {run.date.year}
				</Typography>
				<Chip
					size="small"
					label={statusLabels[run.status]}
					color={statusTones[run.status]}
					variant={statusTones[run.status] === "default" ? "outlined" : "filled"}
				/>
			</Stack>

			{active && <LinearProgress />}

			<Mono sx={{ color: "text.disabled" }}>
				{active
					? `Started ${formatInstant(run.startedAt)}`
					: `${formatInstant(run.startedAt)} · ${run.total} anime${run.total === 1 ? "" : "s"} · ${formatDuration(run)}`}
			</Mono>

			{run.error && (
				<Typography variant="caption" color="error.main">
					{run.error}
				</Typography>
			)}
		</Stack>
	);
}

export function RefreshRunsDrawer({ open, onClose, runs, isPending, error }: Props) {
	return (
		<Drawer anchor="right" open={open} onClose={onClose}>
			<Box sx={{ width: { xs: 320, sm: 400 }, p: 3 }}>
				<Typography variant="h5" sx={{ mb: 2.5 }}>
					Refreshes
				</Typography>

				{isPending && <LinearProgress />}

				{error && <Alert severity="error">Could not load refreshes: {error.message}</Alert>}

				{runs && runs.length === 0 && (
					<Alert severity="info">No refresh recorded yet.</Alert>
				)}

				<Stack divider={<Divider />} spacing={2}>
					{runs?.map((run) => (
						<RunCard key={run.runId} run={run} />
					))}
				</Stack>
			</Box>
		</Drawer>
	);
}
