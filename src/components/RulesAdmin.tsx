import { useCallback, useEffect, useState, type FormEvent } from "react";
import { api } from "../api/client";
import { errorMessage, isUnauthorized } from "../utils/errors";
import type { RulePresetResponse, RuleResponse } from "../api/api";

interface Props {
    onUnauthorized: () => void;
}

// The system owner changes a rule from a start date. Old values are kept, so the history is shown too.
export function RulesAdmin({ onUnauthorized }: Props) {
    const [rules, setRules] = useState<RuleResponse[] | null>(null);
    const [key, setKey] = useState("");
    const [value, setValue] = useState("");
    const [effectiveFrom, setEffectiveFrom] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const [presets, setPresets] = useState<RulePresetResponse[]>([]);
    const [presetId, setPresetId] = useState("");
    const [presetFrom, setPresetFrom] = useState("");
    const fail = useCallback(
        (err: unknown) => (isUnauthorized(err) ? onUnauthorized() : setError(errorMessage(err))),
        [onUnauthorized],
    );

    const load = useCallback(async () => {
        try {
            setRules((await api.api.getRules()).data);
            setPresets((await api.api.getRulePresets()).data);
        } catch (err) {
            fail(err);
        }
    }, [fail]);

    useEffect(() => {
        void load();
    }, [load]);

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setMessage(null);
        setError(null);
        try {
            await api.api.changeRule(key, { value, effectiveFrom});

            setMessage(`${key} is ${value} from ${effectiveFrom}.`);
            setValue("");
            setEffectiveFrom("");
            
            await load();

            
        } catch (err) {
            fail(err);
        }
    }
    async function applyPreset(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setMessage(null);
        setError(null);
        try {
            await api.api.applyRulePreset(presetId, { effectiveFrom: presetFrom });
            setMessage(`The preset was applied from ${presetFrom}.`);
            setPresetId("");
            setPresetFrom("");
            await load();
        } catch (err) {
            fail(err);
        }
    }

    if (!rules) return null;

    return (
        <>
            <form className="form" onSubmit={applyPreset}>
                <label>
                    Preset
                    <select value={presetId} onChange={(e) => setPresetId(e.target.value)} required>
                        <option value="" disabled>Choose a preset</option>
                        {presets.map((p) => (
                            <option key={p.id} value={p.id}>{p.name}</option>
                        ))}
                    </select>
                </label>
                <label>
                    From
                    <input type="date" value={presetFrom} onChange={(e) => setPresetFrom(e.target.value)} required />
                </label>
                <button type="submit" disabled={!presetId}>Apply preset</button>
            </form>

            <form className="form" onSubmit={submit}>
                <label>
                    Rule
                    <select value={key} onChange={(e) => setKey(e.target.value)} required>
                        <option value="" disabled>Choose a rule</option>
                        {rules.map((r) => (
                            <option key={r.key} value={r.key}>{r.key}</option>
                        ))}
                    </select>
                </label>
                <label>
                    New value
                    <input value={value} onChange={(e) => setValue(e.target.value)} required />
                </label>
                <label>
                    From
                    <input type="date" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} required />
                </label>

                {error && <p className="error" role="alert">{error}</p>}
                {message && <p className="success">{message}</p>}

                <button type="submit" disabled={!key}>Save new value</button>
            </form>

            <table>
                <thead>
                <tr>
                    <th>Rule</th>
                    <th>Description</th>
                    <th>Values (newest first)</th>
                </tr>
                </thead>
                <tbody>
                {rules.map((r) => (
                    <tr key={r.key}>
                        <td>{r.key}</td>
                        <td>{r.description}</td>
                        <td>
                            {r.values?.map((v) => (
                                <div key={v.effectiveFrom}>{v.value} from {v.effectiveFrom}</div>
                            ))}
                        </td>
                    </tr>
                ))}
                </tbody>
            </table>
        </>
    );
}