docker run -i \
    -p 5000:5000 \
    -v $SLSK_OUTPUT_DIR:/var/slsk/download \
    -v $SLSK_SHARED_DIR:/var/slsk/shared \
    -e "SLSK_USERNAME=$SLSK_USERNAME" \
    -e "SLSK_PASSWORD=$SLSK_PASSWORD" \
    -e "SLSK_LISTEN_PORT=$SLSK_LISTEN_PORT" \
    slsk-web:latest