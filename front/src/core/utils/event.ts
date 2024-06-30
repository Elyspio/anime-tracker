type Event = {
	[key in string]: (...args: any) => void;
};

export class EventManager<T extends Event> {
	private internal = new EventTarget();

	public on<event extends keyof T>(evt: event, callback: T[event]) {
		this.internal.addEventListener(evt as string, (e) => callback.call(callback, ...(e as CustomEvent).detail));
	}

	public emit<event extends keyof T>(evt: event, ...params: Parameters<T[event]>) {
		console.log("EventManager emit", evt);
		this.internal.dispatchEvent(new CustomEvent(evt as string, { detail: params }));
	}
}
