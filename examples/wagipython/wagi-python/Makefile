LOGDIR   := _scratch/logs
CACHEDIR := _scratch/cache

.PHONY: serve
serve:
	RUST_LOG=wagi=trace wagi -e 'PYTHONHOME=/opt/wasi-python/lib/python3.11' -e 'PYTHONPATH=/opt/wasi-python/lib/python3.11' -c modules.toml --log-dir ${LOGDIR} --module-cache ${CACHEDIR}

.PHONY: run-wasmtime
run-wasmtime:
	wasmtime run opt/wasi-python/bin/python3.wasm --mapdir /::./ --env 'PYTHONHOME=/opt/wasi-python/lib/python3.11' --env 'PYTHONPATH=/opt/wasi-python/lib/python3.11' -- code/env.py

.PHONY: tail-logs
tail-logs:
	tail -f ${LOGDIR}/*/*
