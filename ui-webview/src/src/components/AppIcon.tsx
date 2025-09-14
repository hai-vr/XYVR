import {Component} from "react";
import {NamedApp, type NamedAppType} from "../types/CoreTypes.ts";
import resoniteIcon from "../assets/Resonite_Wiki-Icon.png";
import cvrIcon from "../assets/cvr_logo_small.png";

interface AppIconProps {
    namedApp: NamedAppType
}

export class AppIcon extends Component<AppIconProps> {
    render() {
        const { namedApp } = this.props;

        const getAppIconClass = (namedApp: NamedAppType) => {
            switch (namedApp) {
                case NamedApp.Resonite:
                    return "app-icon resonite";
                case NamedApp.VRChat:
                    return "app-icon vrchat";
                case NamedApp.Cluster:
                    return "app-icon cluster";
                case NamedApp.ChilloutVR:
                    return "app-icon chilloutvr";
                default:
                    return "app-icon default";
            }
        };

        const getAppIcon = (namedApp: NamedAppType) => {
            switch (namedApp) {
                case NamedApp.Resonite:
                    return <img src={resoniteIcon} alt="Resonite" className="app-icon-img" title="Resonite"/>;
                case NamedApp.VRChat:
                    return '💬';
                case NamedApp.Cluster:
                    return '☁️';
                case NamedApp.ChilloutVR:
                    return <img src={cvrIcon} alt="ChilloutVR" className="app-icon-img" title="ChilloutVR"/>;
                default:
                    return '❓';
            }
        };

        return (<div className={getAppIconClass(namedApp)}>
            {getAppIcon(namedApp)}
        </div>)
    }
}