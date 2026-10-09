#include <stdio.h>
#include <linux/uinput.h>
#include <linux/input.h>
#include <linux/input-event-codes.h>

int main(void) {
    printf("UI_DEV_CREATE=0x%08lx\n", (unsigned long)UI_DEV_CREATE);
    printf("UI_DEV_DESTROY=0x%08lx\n", (unsigned long)UI_DEV_DESTROY);
    printf("UI_SET_EVBIT=0x%08lx\n", (unsigned long)UI_SET_EVBIT);
    printf("UI_SET_KEYBIT=0x%08lx\n", (unsigned long)UI_SET_KEYBIT);
    printf("UI_SET_RELBIT=0x%08lx\n", (unsigned long)UI_SET_RELBIT);
    printf("UI_SET_ABSBIT=0x%08lx\n", (unsigned long)UI_SET_ABSBIT);
    printf("UI_DEV_SETUP=0x%08lx\n", (unsigned long)UI_DEV_SETUP);
    printf("UI_ABS_SETUP=0x%08lx\n", (unsigned long)UI_ABS_SETUP);
    printf("sizeof(struct uinput_setup)=%zu\n", sizeof(struct uinput_setup));
    printf("sizeof(struct uinput_abs_setup)=%zu\n", sizeof(struct uinput_abs_setup));
    printf("sizeof(struct input_id)=%zu\n", sizeof(struct input_id));
    printf("sizeof(struct input_absinfo)=%zu\n", sizeof(struct input_absinfo));
    printf("sizeof(struct input_event)=%zu\n", sizeof(struct input_event));
    printf("sizeof(struct timeval)=%zu\n", sizeof(struct timeval));
    printf("UINPUT_MAX_NAME_SIZE=%d\n", UINPUT_MAX_NAME_SIZE);
    printf("---offsets uinput_setup---\n");
    printf("id off=%zu name off=%zu ff_effects_max off=%zu\n",
        __builtin_offsetof(struct uinput_setup, id),
        __builtin_offsetof(struct uinput_setup, name),
        __builtin_offsetof(struct uinput_setup, ff_effects_max));
    printf("---offsets uinput_abs_setup---\n");
    printf("code off=%zu absinfo off=%zu\n",
        __builtin_offsetof(struct uinput_abs_setup, code),
        __builtin_offsetof(struct uinput_abs_setup, absinfo));
    printf("---offsets input_event---\n");
    printf("time off=%zu type off=%zu code off=%zu value off=%zu\n",
        __builtin_offsetof(struct input_event, time),
        __builtin_offsetof(struct input_event, type),
        __builtin_offsetof(struct input_event, code),
        __builtin_offsetof(struct input_event, value));
    printf("---event/type codes---\n");
    printf("EV_SYN=0x%02x EV_KEY=0x%02x EV_REL=0x%02x EV_ABS=0x%02x\n", EV_SYN, EV_KEY, EV_REL, EV_ABS);
    printf("SYN_REPORT=0x%02x\n", SYN_REPORT);
    printf("REL_X=0x%02x REL_Y=0x%02x REL_WHEEL=0x%02x\n", REL_X, REL_Y, REL_WHEEL);
    printf("ABS_X=0x%02x ABS_Y=0x%02x\n", ABS_X, ABS_Y);
    printf("BTN_LEFT=0x%03x BTN_RIGHT=0x%03x BTN_MIDDLE=0x%03x\n", BTN_LEFT, BTN_RIGHT, BTN_MIDDLE);
    printf("KEY_A=0x%03x KEY_ESC=0x%03x KEY_MAX=0x%03x\n", KEY_A, KEY_ESC, KEY_MAX);
    printf("BUS_VIRTUAL=0x%04x\n", BUS_VIRTUAL);
    return 0;
}
