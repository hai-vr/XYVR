import {Component} from "react";
import {type NamedAppType} from "../types/CoreTypes.ts";
import clsx from "clsx";
import {SupportedAppsByNamedApp} from "../supported-apps.tsx";

interface AppIconProps {
    namedApp: NamedAppType,
    mini?: boolean
}

export class AppIcon extends Component<AppIconProps> {
    render() {
        const {namedApp, mini} = this.props;

        const supportedApp = SupportedAppsByNamedApp[namedApp];

        return (<div className={clsx('app-icon', supportedApp?.iconClass, {
            'app-icon-mini': mini
        })}>
            {supportedApp?.getIcon() || '❓'}
        </div>)
    }
}