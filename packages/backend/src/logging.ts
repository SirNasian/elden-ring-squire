const log = (message: string, ...meta: unknown[]) => {
	const date = new Date();
	console.log(JSON.stringify([date, message, ...meta]))
};

export const logger = { log };
export default logger;
