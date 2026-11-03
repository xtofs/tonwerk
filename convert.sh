#!/bin/sh

input="$1"
output="${input%.*}.m4a"
ffmpeg -i "$input" -c:a aac -b:a 128k "$output"