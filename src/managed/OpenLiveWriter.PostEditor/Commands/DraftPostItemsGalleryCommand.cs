// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Drawing;
using System.IO;
using OpenLiveWriter.ApplicationFramework;
using OpenLiveWriter.BlogClient;
using OpenLiveWriter.Localization;

namespace OpenLiveWriter.PostEditor.Commands
{
    public class DraftPostItemsGalleryCommand : GalleryCommand<Command>
    {
        private IBlogPostEditingSite postEditingSite;
        private PostInfo[] postInfo = new PostInfo[0];
        private bool _isPost;
        // Note: The maximum number of recent items specified here should match the RecentItem's MaxCount attribute in ribbon.xml.
        private const int MaxItems = PostListCache.MaxItems;
        private Command[] _commands = new Command[MaxItems];
        private object _commandsLock = new object();
        private int draftCmdStart = (int)CommandId.OpenDraftMRU0;

        public DraftPostItemsGalleryCommand(IBlogPostEditingSite postEditingSite, CommandManager commandManager, bool isPost)
            : base((isPost ? CommandId.OpenPostSplit : CommandId.OpenDraftSplit), false)
        {
            this.postEditingSite = postEditingSite;
            _isPost = isPost;
            if (isPost)
            {
                draftCmdStart = (int)CommandId.OpenPostMRU0;
            }

            // These rows are one-shot actions (open a draft/post), not a persistent selection,
            // so don't have the framework try to track/highlight a "selected" gallery item.
            AllowSelection = false;
            ExecuteWithArgs += new ExecuteEventHandler(DraftPostItemsGalleryCommand_ExecuteWithArgs);

            lock (_commandsLock)
            {
                // The framework does not re-query UI_PKEY_Label/UI_PKEY_LargeImage for
                // "command type" (Action) gallery items referencing these commands - it only
                // uses them to route Execute by CommandId, using whatever Label/Image was
                // declared for them once in Ribbon.xml. All per-row display data below is
                // instead carried directly on each GalleryItem (see LoadItems()), which is
                // the pattern BlogProviderButtonManager already relies on successfully.
                for (int i = 0; i < _commands.Length; i++)
                {
                    _commands[i] = new Command((CommandId)(i + draftCmdStart));
                    _commands[i].CommandBarButtonStyle = CommandBarButtonStyle.Provider;
                    _commands[i].On = false;
                }

                commandManager.Add(_commands);
                commandManager.Add(this);
            }
        }

        private void DraftPostItemsGalleryCommand_ExecuteWithArgs(object sender, EventArgs e)
        {
            int index = SelectedIndex;
            if (index >= 0 && index < postInfo.Length)
            {
                WindowCascadeHelper.SetNextOpenedLocation(postEditingSite.FrameWindow.Location);
                postEditingSite.OpenLocalPost(postInfo[index]);
            }
        }

        public override void LoadItems()
        {
            items.Clear();
            selectedIndex = INVALID_INDEX;

            postInfo = (_isPost ? PostListCache.RecentPosts : PostListCache.Drafts);

            lock (_commandsLock)
            {
                for (int i = 0; i < _commands.Length && i < postInfo.Length; i++)
                {
                    PostInfo v = postInfo[i];
                    _commands[i].On = true;
                    _commands[i].Enabled = true;
                    _commands[i].LabelTitle = v.Title;
                    _commands[i].LabelDescription = v.BlogName;
                    _commands[i].TooltipTitle = v.Title;
                    _commands[i].TooltipDescription = v.BlogName;

                    Bitmap image;
                    using (BlogSettings bs = BlogSettings.ForBlogId(v.BlogId))
                    {
                        image = bs.ClientType.Contains("WordPress") ? Images.WordPressPost_LargeImage
                                                                     : Images.OtherBlogPost_LargeImage;
                    }
                    _commands[i].LargeImage = image;

                    // GalleryItem disposes the bitmap it's given, and Images.* getters return a
                    // shared cached instance, so hand it a private clone rather than the original.
                    items.Add(new GalleryItem(v.Title, new Bitmap(image), _commands[i]));
                }
            }

            base.LoadItems();
        }
    }
}
