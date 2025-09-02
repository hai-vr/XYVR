import Account from "./Account.jsx";
import "./Individual.css";

function Individual({ individual, index, isVisible = true, showBio = false }) {
    // Get all VRChat account links and filter to only show http/https URLs
    const vrChatLinks = individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.urls?.length > 0)
        ?.flatMap(account => account.specifics.urls)
        // Some users have links that are an empty string. We don't want this because clicking it causes the page to reload.
        // Generally, prevent links that aren't http:// nor https://
        ?.filter(url => url && (url.startsWith('http://') || url.startsWith('https://'))) || [];

    // Get all VRChat account bios
    const vrcBios = showBio && individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.bio)
        ?.map(account => account.specifics.bio)
        ?.filter(bio => bio.trim() !== '') || [];

    const copyToClipboard = async (url, event) => {
        event.stopPropagation(); // Prevent the container click event
        await navigator.clipboard.writeText(url);
    };

    return (
        <div className={`individual-container ${!isVisible ? 'hidden' : ''}`}>
            <div className="individual-header">
                <div className="individual-avatar">
                    {individual.displayName?.charAt(0).toUpperCase() || '?'}
                </div>
                <h3 className="individual-name">
                    {individual.displayName}
                </h3>
                {individual.isAnyContact && (
                    <span className="contact-badge">
                        📞 Contact
                    </span>
                )}
            </div>

            <div className="accounts-container">
                {individual.accounts && individual.accounts.length > 0 ? (
                    <div className="accounts-grid">
                        {individual.accounts.map((account, accountIndex) => (
                            <Account key={accountIndex} account={account} />
                        ))}
                    </div>
                ) : (
                    <div className="no-accounts">
                        📭 No accounts found
                    </div>
                )}
            </div>

            {vrChatLinks.length > 0 && (
                <div className="vrchat-links-list">
                    {vrChatLinks.map((url, linkIndex) => (
                        <div key={linkIndex} className="vrchat-link-item">
                            <a
                                href={url}
                                rel="noopener noreferrer"
                                className="vrchat-link"
                            >
                                🔗 {url}
                            </a>
                            <button
                                onClick={(e) => copyToClipboard(url, e)}
                                className="copy-button"
                                title="Copy link to clipboard"
                            >
                                📋
                            </button>
                        </div>
                    ))}
                </div>
            )}

            {vrcBios.length > 0 && showBio && (
                <div className="vrchat-bios-container">
                    <div className="vrchat-links-list">
                        {vrcBios.map((bio, bioIndex) => (
                            <div key={bioIndex} className="vrchat-bio-item">
                                {bio.split('\n').map((line, lineIndex) => (
                                    <span key={lineIndex}>
                                        {line}
                                        {lineIndex < bio.split('\n').length - 1 && <br/>}
                                    </span>
                                ))}
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

export default Individual;