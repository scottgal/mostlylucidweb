# Native dependencies

This package redistributes sqlite-vector-rs 0.3.1 unchanged from
https://github.com/quinnjr/sqlite-vector-rs at commit
7ee2baf7b2783c1798f285cdff3b2285da38de04, under its MIT license option.
The upstream Cargo.lock pins its dependencies, including USearch and Arrow.
The build retains dependency SPDX declarations and available license/notice texts under licenses/ in the package. See licenses/dependencies.json for exact versions and source repositories.
No upstream affiliation or endorsement is implied.

Windows assets include documented dependency compatibility modifications: sqlite3_ext 0.2.0 gains a Windows UTF-8 path helper; USearch 2.24.0 uses the Windows allocator failure condition and disables optional SIMD/FP16 features. The original source checksums and modifications are retained in licenses/windows-compatibility.json. Dependency versions are unchanged.
