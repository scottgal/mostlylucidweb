#!/usr/bin/env bash
set -euo pipefail
# Build the pinned extension; Windows compatibility patches affect private dependency copies only.
repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
rid="${1:?usage: build-sqlite-vector.sh <osx-arm64|linux-x64|win-x64> [work-directory]}"
case "$rid" in
  osx-arm64) target=aarch64-apple-darwin; library=libsqlite_vector_rs.dylib ;;
  linux-x64) target=x86_64-unknown-linux-gnu; library=libsqlite_vector_rs.so ;;
  win-x64) target=x86_64-pc-windows-msvc; library=sqlite_vector_rs.dll ;;
  *) echo "Unsupported runtime: $rid" >&2; exit 1 ;;
esac
source_commit=7ee2baf7b2783c1798f285cdff3b2285da38de04
work="${2:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/mostlylucid-sqlite-vector-source}"
if [[ ! -d "$work/.git" ]]; then
  git clone --no-checkout https://github.com/quinnjr/sqlite-vector-rs.git "$work"
fi
git -C "$work" checkout --detach "$source_commit"
# Reset only the private build manifests before reapplying platform compatibility.
git -C "$work" restore --source="$source_commit" -- Cargo.toml Cargo.lock
rustup target add "$target"
if [[ "$rid" == "win-x64" ]]; then
  cargo fetch --manifest-path "$work/Cargo.toml" --locked --target "$target"
  python3 "$repo_root/scripts/relevance/prepare-windows-native.py" "$work"
fi
cargo build --manifest-path "$work/Cargo.toml" --release --locked --target "$target"
destination="$repo_root/Mostlylucid.Sqlite.Vector/runtimes/$rid/native"
mkdir -p "$destination"
cp "$work/target/$target/release/$library" "$destination/$library"

python3 "$repo_root/scripts/relevance/collect-native-notices.py" "$work" "$repo_root/Mostlylucid.Sqlite.Vector/runtimes/licenses"
if [[ "$rid" == "win-x64" ]]; then
  cp "$work/windows-compatibility.json" "$repo_root/Mostlylucid.Sqlite.Vector/runtimes/licenses/windows-compatibility.json"
fi
