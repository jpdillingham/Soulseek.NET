const baseUrl = process.env.NODE_ENV === 'production' ? 'api/v1' : 'http://localhost:5000/api/v1'

export {
    baseUrl,
}