import { MenuItem, Select } from "@mui/material";
import { adultFilterLabels, adultFilters, type AdultFilter } from "@/core/ranking";
import { FilterField } from "@/view/components/FilterField";

interface Props {
	value: AdultFilter;
	onChange: (next: AdultFilter) => void;
}

/** Borderless: the FilterField around it already draws the box. */
const bareSelect = {
	"& .MuiSelect-select": { py: 0, pl: 0 },
	"& .MuiOutlinedInput-notchedOutline": { border: 0 },
	"&:hover .MuiOutlinedInput-notchedOutline": { border: 0 },
	"&.Mui-focused .MuiOutlinedInput-notchedOutline": { border: 0 },
};

/** Adult entries hidden, shown with the rest, or shown alone. The same control on a season and a filmography. */
export function AdultSelect({ value, onChange }: Props) {
	return (
		<FilterField label="Adult">
			<Select value={value} onChange={(event) => onChange(event.target.value as AdultFilter)} sx={bareSelect} inputProps={{ "aria-label": "Adult entries" }}>
				{adultFilters.map((filter) => (
					<MenuItem key={filter} value={filter}>
						{adultFilterLabels[filter]}
					</MenuItem>
				))}
			</Select>
		</FilterField>
	);
}
