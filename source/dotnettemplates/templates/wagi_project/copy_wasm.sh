#!/bin/sh
# This file is only used to set up the projet and can be safely deleted.
set -eux
if [ -f source_wasm_file ]; then
  mkdir -p $(dirname dest_wasm_file)
  cp source_wasm_file dest_wasm_file
else
  >&2 echo source_wasm_file does not exist
  exit 1
fi