#!/bin/sh

set -u

write_error() {
    printf '{"success":false,"message":"%s"}\n' "$1"
    exit 1
}

usage() {
    write_error "Usage: sh ./atm.sh <tool_name> ['<json_parameters>'] [--parameters-file <path>] [--timeout <seconds>]"
}

json_first_character() {
    printf '%s' "$1" | awk '{ line = $0; sub(/^[[:space:]]*/, "", line); if (length(line) > 0) { printf "%s", substr(line, 1, 1); exit } }'
}

is_json_object() {
    printf '%s' "$1" | awk '
        {
            line = $0
            if (first == "") {
                sub(/^[[:space:]]*/, "", line)
                if (length(line) > 0) first = substr(line, 1, 1)
            }
            sub(/[[:space:]]*$/, "", line)
            if (length(line) > 0) last = substr(line, length(line), 1)
        }
        END {
            if (NR > 0 && first == "{" && last == "}") exit 0
            exit 1
        }
    '
}

cleanup() {
    if [ -n "${command_tmp:-}" ] && [ -f "$command_tmp" ]; then rm -f "$command_tmp"; fi
    if [ "${command_queued:-false}" = true ] && [ -n "${expected_tmp:-}" ] && [ -f "$expected_tmp" ] && [ -f "${command_path:-}" ] && cmp -s "$expected_tmp" "$command_path"; then
        rm -f "$command_path"
    fi
    if [ -n "${expected_tmp:-}" ] && [ -f "$expected_tmp" ]; then rm -f "$expected_tmp"; fi
}

trap cleanup 0
trap 'exit 1' HUP INT TERM

if [ "$#" -lt 1 ]; then usage; fi

first_argument=$1
shift
command_json=""
parameters="{}"
parameters_file=""
parameters_from_stdin=false
timeout_seconds=30

if [ "$(json_first_character "$first_argument")" = "{" ]; then
    if ! is_json_object "$first_argument"; then
        write_error "Full command input must be one JSON object."
    fi
    command_json=$first_argument
    while [ "$#" -gt 0 ]; do
        case "$1" in
            --timeout|-TimeoutSeconds)
                shift
                if [ "$#" -eq 0 ]; then write_error "Timeout value is missing."; fi
                timeout_seconds=$1
                shift
                ;;
            *) write_error "Full command input accepts only a timeout option." ;;
        esac
    done
else
    tool_name=$first_argument
    case "$tool_name" in
        ''|*[!A-Za-z0-9_]*) write_error "Tool name must contain only letters, numbers, and underscores." ;;
    esac

    parameters_set=false
    while [ "$#" -gt 0 ]; do
        case "$1" in
            --parameters-file|-ParametersFile)
                shift
                if [ "$#" -eq 0 ]; then write_error "Parameters file path is missing."; fi
                if [ "$parameters_set" = true ]; then write_error "Specify parameters only once."; fi
                parameters_file=$1
                parameters_set=true
                shift
                ;;
            --timeout|-TimeoutSeconds)
                shift
                if [ "$#" -eq 0 ]; then write_error "Timeout value is missing."; fi
                timeout_seconds=$1
                shift
                ;;
            -)
                if [ "$parameters_set" = true ]; then write_error "Specify parameters only once."; fi
                parameters_from_stdin=true
                parameters_set=true
                shift
                ;;
            *)
                if [ "$parameters_set" = true ]; then write_error "Unexpected argument; use --parameters-file for complex parameters."; fi
                parameters=$1
                parameters_set=true
                shift
                ;;
        esac
    done

    case "$timeout_seconds" in
        ''|*[!0-9]*) write_error "Timeout must be a whole number from 1 to 3600 seconds." ;;
    esac
    if [ "$timeout_seconds" -lt 1 ] || [ "$timeout_seconds" -gt 3600 ]; then
        write_error "Timeout must be a whole number from 1 to 3600 seconds."
    fi

    if [ -n "$parameters_file" ]; then
        if [ ! -f "$parameters_file" ]; then write_error "Parameters file was not found."; fi
        parameters=$(LC_ALL=C awk 'NR == 1 { sub(/^\357\273\277/, "") } { if (NR > 1) printf "\n"; printf "%s", $0 }' "$parameters_file") || write_error "Could not read parameters file."
    elif [ "$parameters_from_stdin" = true ]; then
        parameters=$(LC_ALL=C awk 'NR == 1 { sub(/^\357\273\277/, "") } { if (NR > 1) printf "\n"; printf "%s", $0 }') || write_error "Could not read parameters from stdin."
    fi

    if ! is_json_object "$parameters"; then
        write_error "Parameters must be one JSON object; use --parameters-file for complex input."
    fi
    command_json=$(printf '{"tool":"%s","parameters":%s}' "$tool_name" "$parameters")
fi

case "$timeout_seconds" in
    ''|*[!0-9]*) write_error "Timeout must be a whole number from 1 to 3600 seconds." ;;
esac
if [ "$timeout_seconds" -lt 1 ] || [ "$timeout_seconds" -gt 3600 ]; then
    write_error "Timeout must be a whole number from 1 to 3600 seconds."
fi

project_root=$(CDPATH= cd "$(dirname "$0")" 2>/dev/null && pwd -P) || write_error "Could not locate the Unity project root."
project_settings_dir="$project_root/ProjectSettings/AwesomeTaskManager"
if [ ! -d "$project_settings_dir" ]; then
    write_error "ATM Next data folder was not found; run this helper from a Unity project containing ATM Next."
fi

if [ ! -f "$project_root/Temp/UnityLockfile" ]; then
    if ! command -v pgrep >/dev/null 2>&1 || ! pgrep -x Unity >/dev/null 2>&1; then
        write_error "Unity Editor is not running. Start the project in Unity and retry; do not edit ATM data files directly."
    fi
fi

command_path="$project_settings_dir/external_command.json"
result_path="$project_settings_dir/external_result.json"
if [ -f "$command_path" ]; then
    write_error "An ATM command is already pending. Wait for Unity to consume it; do not overwrite it or edit ATM data files."
fi

if [ -f "$result_path" ]; then rm -f "$result_path" || write_error "Could not clear the previous ATM result."; fi

umask 077
command_tmp="$project_settings_dir/.atm-command.$$.tmp"
expected_tmp="$project_settings_dir/.atm-expected.$$.tmp"
if [ -e "$command_tmp" ] || [ -e "$expected_tmp" ]; then
    write_error "Could not create temporary ATM bridge files; retry the command."
fi
if ! printf '%s' "$command_json" > "$command_tmp" || ! cp "$command_tmp" "$expected_tmp"; then
    write_error "Could not prepare the ATM command."
fi
if ! mv "$command_tmp" "$command_path"; then
    write_error "Could not queue the ATM command; another command may be pending."
fi
command_tmp=""
command_queued=true

elapsed=0
while [ "$elapsed" -lt "$timeout_seconds" ]; do
    sleep 1
    elapsed=$((elapsed + 1))
    if [ -f "$result_path" ]; then
        result_json=$(cat "$result_path")
        if [ -n "$result_json" ] && is_json_object "$result_json"; then
            printf '%s\n' "$result_json"
            command_queued=false
            exit 0
        fi
    fi
done

write_error "ATM bridge timed out; its unconsumed command was removed if still pending. Confirm Unity is open and not compiling, then retry; never edit ATM data files."