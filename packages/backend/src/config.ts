const HOST = process.env.HOST ?? "localhost";
const PORT = isNaN(parseInt(process.env.PORT)) ? 5000 : parseInt(process.env.PORT);

export const config = { HOST, PORT };
export default config;
