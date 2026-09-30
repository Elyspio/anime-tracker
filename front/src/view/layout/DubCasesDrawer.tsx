import { useState } from "react";
import { Alert, Box, Button, Divider, Drawer, LinearProgress, Link, Stack, TextField, Typography } from "@mui/material";
import { useSnackbar } from "notistack";
import { useDubCases } from "@/core/api/queries";
import { useSetDubOverride } from "@/core/api/mutations";
import { caseReason, platformNames } from "@/core/dub";
import { seasonLabels } from "@/core/binge";
import { Eyebrow } from "@/view/components/Eyebrow";
import type { AnimeSeason, DubCase, DubOverrideRequest } from "@/core/api/types";

interface Props {
	open: boolean;
	onClose: () => void;
	year: number;
	season: AnimeSeason;
}

/**
 * The dub matches an admin should look at, and the three answers they can give: this series, no series,
 * or back to the automatic match. Shown to anyone signed in — the API says who may actually change
 * one, and its refusal is what gets rendered.
 */
export function DubCasesDrawer({ open, onClose, year, season }: Props) {
	const cases = useDubCases(year, season, open);

	return (
		<Drawer anchor="right" open={open} onClose={onClose}>
			<Box sx={{ width: { xs: 340, sm: 460 }, p: 3 }}>
				<Typography variant="h5">French dub matches</Typography>
				<Typography variant="body2" sx={{ color: "text.secondary", mb: 2.5 }}>
					{seasonLabels[season]} {year}: matches made by title alone, series that do not line up, and every correction.
				</Typography>

				{cases.isPending && <LinearProgress />}

				{cases.error && <Alert severity="error">Could not load the matches: {cases.error.message}</Alert>}

				{cases.data && cases.data.length === 0 && <Alert severity="info">Nothing to look at for this season.</Alert>}

				<Stack divider={<Divider />} spacing={2}>
					{cases.data?.map((dubCase) => (
						<CaseCard key={`${dubCase.sourceId}-${dubCase.platform}`} dubCase={dubCase} year={year} season={season} />
					))}
				</Stack>
			</Box>
		</Drawer>
	);
}

function CaseCard({ dubCase, year, season }: { dubCase: DubCase; year: number; season: AnimeSeason }) {
	const [url, setUrl] = useState("");
	const override = useSetDubOverride({ year, season });
	const { enqueueSnackbar } = useSnackbar();

	const send = (request: DubOverrideRequest) =>
		override.mutate(
			{ sourceId: dubCase.sourceId, platform: dubCase.platform, request },
			{
				onSuccess: (result) => {
					if (result.error) enqueueSnackbar(result.error, { variant: "warning" });
					else enqueueSnackbar(`${dubCase.title}: ${caseReason(result.case)}`, { variant: "info" });
					setUrl("");
				},
				onError: (error) => enqueueSnackbar(error instanceof Error ? error.message : "The correction was refused", { variant: "error" }),
			}
		);

	return (
		<Stack spacing={1}>
			<Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "baseline", gap: 1 }}>
				<Typography variant="subtitle2" sx={{ minWidth: 0 }} noWrap title={dubCase.title}>
					{dubCase.title}
				</Typography>
				<Eyebrow>{platformNames[dubCase.platform]}</Eyebrow>
			</Stack>

			<Typography variant="body2" sx={{ color: "text.secondary" }}>
				{caseReason(dubCase)}
			</Typography>

			{dubCase.seriesUrl && (
				<Link href={dubCase.seriesUrl} target="_blank" rel="noopener noreferrer" variant="body2">
					{dubCase.seriesTitle ?? dubCase.seriesUrl}
				</Link>
			)}

			<Stack direction="row" sx={{ gap: 1, alignItems: "center" }}>
				<TextField
					size="small"
					value={url}
					onChange={(event) => setUrl(event.target.value)}
					placeholder={`${platformNames[dubCase.platform]} series page`}
					sx={{ flex: 1 }}
					slotProps={{ htmlInput: { "aria-label": `${platformNames[dubCase.platform]} series page for ${dubCase.title}` } }}
				/>
				<Button size="small" variant="outlined" disabled={url.trim() === ""} loading={override.isPending} onClick={() => send({ mode: "Pinned", url: url.trim() })}>
					Pin
				</Button>
			</Stack>

			<Stack direction="row" sx={{ gap: 1 }}>
				{dubCase.override !== "Blocked" && (
					<Button size="small" disabled={override.isPending} onClick={() => send({ mode: "Blocked" })}>
						Not on {platformNames[dubCase.platform]}
					</Button>
				)}
				{dubCase.override !== "Auto" && (
					<Button size="small" disabled={override.isPending} onClick={() => send({ mode: "Auto" })}>
						Match automatically
					</Button>
				)}
			</Stack>
		</Stack>
	);
}
