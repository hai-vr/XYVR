import {Component} from "react";
import {NamedApp, type NamedAppType} from "../types/CoreTypes.ts";
import resoniteIcon from "../assets/Resonite_Wiki-Icon.png";
import clusterIcon from "../assets/cluster.png";
import cvrIcon from "../assets/cvr_logo_small.png";
import clsx from "clsx";

interface AppIconProps {
    namedApp: NamedAppType,
    mini?: boolean
}

export class AppIcon extends Component<AppIconProps> {
    render() {
        const {namedApp, mini} = this.props;

        const getAppIcon = (namedApp: NamedAppType) => {
            switch (namedApp) {
                case NamedApp.Resonite:
                    return <img src={resoniteIcon} alt="Resonite" className="app-icon-img" title="Resonite"/>;
                case NamedApp.VRChat:
                    return '💬';
                case NamedApp.Cluster:
                    return <img src={clusterIcon} alt="cluster" className="app-icon-img" title="Cluster"/>;
                case NamedApp.ChilloutVR:
                    return <img src={cvrIcon} alt="ChilloutVR" className="app-icon-img" title="ChilloutVR"/>;
                default:
                    return '❓';
            }
        };

        return (<div className={clsx('app-icon', {
            'resonite': namedApp === NamedApp.Resonite,
            'vrchat': namedApp === NamedApp.VRChat,
            'cluster': namedApp === NamedApp.Cluster,
            'chilloutvr': namedApp === NamedApp.ChilloutVR,
            'app-icon-mini': mini
        })}>
            {getAppIcon(namedApp)}
        </div>)
    }
}