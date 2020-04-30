const baseUrl = process.env.NODE_ENV === 'production' ? 'api/v1' : 'http://localhost:5000/api/v1';
const tokenKey = 'soulseek-example-token';
const tokenPassthroughValue = JSON.stringify('n/a');

export {
    baseUrl,
    tokenKey,
    tokenPassthroughValue
}