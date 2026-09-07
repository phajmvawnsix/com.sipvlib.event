# Changelog

## [1.1.2] - 2026-09-03

Pin the com.sipvlib.debugging dependency to a semver version (1.1.1) instead of a git URL, for the
same OpenUPM registry-resolution reason as com.sipvlib.utilities 2.0.2.

## [1.1.1] - 2026-09-03

Lower minimum Unity Editor version to 2022.3 LTS (was 6000.3) and add a `repository`
field to `package.json`, both required for OpenUPM registry submission.

## [1.1.0] - 2026-08-18

- Lowered minimum Unity version from `6000.3` to `2022.3` (LTS) to support consuming
  packages targeting legacy Unity projects. No API or behavior changes.

## [1.0.0] - 2026-07-18

Initial extraction from SiPVLib monolith.
